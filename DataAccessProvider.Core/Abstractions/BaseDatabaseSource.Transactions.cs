using System.Data;
using System.Data.Common;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core.Abstractions;

public abstract partial class BaseDatabaseSource<TDatabaseSourceParams>
    where TDatabaseSourceParams : BaseDatabaseSourceParams
{
    protected async Task ExecuteInTransactionCoreAsync(
        Func<IDatabaseTransaction, CancellationToken, Task> operation,
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteInTransactionCoreAsync(
            async (transaction, ct) =>
            {
                await operation(transaction, ct).ConfigureAwait(false);
                return true;
            },
            isolationLevel,
            cancellationToken).ConfigureAwait(false);
    }

    protected async Task<TResult> ExecuteInTransactionCoreAsync<TResult>(
        Func<IDatabaseTransaction, CancellationToken, Task<TResult>> operation,
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        await using var connection = GetConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var transaction = isolationLevel.HasValue
            ? await connection.BeginTransactionAsync(isolationLevel.Value, cancellationToken).ConfigureAwait(false)
            : await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var executor = new DatabaseTransactionExecutor(
            this,
            connection,
            transaction,
            typeof(TDatabaseSourceParams).GetCleanGenericTypeName(),
            cancellationToken);

        TResult result;
        try
        {
            result = await operation(executor, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await executor.DeactivateAndDrainAsync().ConfigureAwait(false);
        }
        catch (Exception operationException)
        {
            await executor.DeactivateAndDrainAsync().ConfigureAwait(false);

            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception rollbackException)
            {
                throw new DatabaseTransactionException(
                    "The transaction operation failed and the subsequent rollback also failed.",
                    DatabaseTransactionFailureStage.Rollback,
                    operationException,
                    rollbackException,
                    commitOutcomeUnknown: false);
            }

            throw;
        }

        try
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (Exception commitException)
        {
            Exception? rollbackException = null;
            try
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                rollbackException = exception;
            }

            throw new DatabaseTransactionException(
                "The transaction commit failed. The commit outcome is unknown and must not be retried automatically.",
                DatabaseTransactionFailureStage.Commit,
                commitException,
                rollbackException,
                commitOutcomeUnknown: true);
        }
    }
}

public abstract partial class BaseDatabaseSource
{
    private async Task<BaseDatabaseSourceParams> ExecuteTransactionReaderAsync(
        BaseDatabaseSourceParams sourceParams,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateTransactionCommand(sourceParams, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var resultSet = new Dictionary<int, List<Dictionary<string, object>>>();
        var resultCount = 0;
        do
        {
            resultSet[resultCount++] = await ReadResultAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

        if (resultSet.Count == 1)
        {
            var firstResultSet = resultSet[0];
            sourceParams.SetValue(firstResultSet.Count switch
            {
                0 => new Dictionary<string, object>(),
                1 => firstResultSet[0],
                _ => firstResultSet
            });
        }
        else if (resultSet.Count > 1)
        {
            sourceParams.SetValue(resultSet);
        }

        return sourceParams;
    }

    private async Task<TSourceParams> ExecuteTransactionReaderAsync<TValue, TSourceParams>(
        TSourceParams sourceParams,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
        where TValue : class, new()
        where TSourceParams : BaseDatabaseSourceParams<TValue>
    {
        await using var command = CreateTransactionCommand(sourceParams, connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var result = await MaterializeAsync<TValue>(reader, cancellationToken).ConfigureAwait(false);

        if (result.Count == 1)
        {
            sourceParams.SetValue(result[0]);
        }
        else if (result.Count > 1)
        {
            sourceParams.SetValue(result);
        }

        return sourceParams;
    }

    private async Task<BaseDatabaseSourceParams> ExecuteTransactionNonQueryAsync(
        BaseDatabaseSourceParams sourceParams,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateTransactionCommand(sourceParams, connection, transaction);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        sourceParams.SetValue(affectedRows);
        sourceParams.AffectedRows = affectedRows;
        return sourceParams;
    }

    private async Task<BaseDatabaseSourceParams> ExecuteTransactionScalarAsync(
        BaseDatabaseSourceParams sourceParams,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = CreateTransactionCommand(sourceParams, connection, transaction);
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        sourceParams.SetValue(result!);
        return sourceParams;
    }

    private DbCommand CreateTransactionCommand(
        BaseDatabaseSourceParams sourceParams,
        DbConnection connection,
        DbTransaction transaction)
    {
        var command = GetCommand(sourceParams.Query, connection);
        ConfigureTransactionCommand(command, sourceParams.CommandType, sourceParams.Timeout, sourceParams.Parameters, transaction);
        return command;
    }

    private DbCommand CreateTransactionCommand<TValue>(
        BaseDatabaseSourceParams<TValue> sourceParams,
        DbConnection connection,
        DbTransaction transaction)
        where TValue : class
    {
        var command = GetCommand(sourceParams.Query, connection);
        ConfigureTransactionCommand(command, sourceParams.CommandType, sourceParams.Timeout, sourceParams.Parameters, transaction);
        return command;
    }

    private void ConfigureTransactionCommand(
        DbCommand command,
        CommandType commandType,
        int commandTimeout,
        IEnumerable<DataAccessParameter>? parameters,
        DbTransaction transaction)
    {
        command.CommandTimeout = commandTimeout;
        command.CommandType = commandType;
        command.Transaction = transaction;

        if (parameters == null)
        {
            return;
        }

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(CreateDbParameter(command, parameter));
        }
    }

    internal sealed class DatabaseTransactionExecutor : IDatabaseTransaction
    {
        private readonly BaseDatabaseSource _source;
        private readonly DbConnection _connection;
        private readonly DbTransaction _transaction;
        private readonly string _expectedParameterFamily;
        private readonly CancellationToken _transactionCancellationToken;
        private readonly SemaphoreSlim _commandGate = new(1, 1);
        private int _active = 1;

        public DatabaseTransactionExecutor(
            BaseDatabaseSource source,
            DbConnection connection,
            DbTransaction transaction,
            string expectedParameterFamily,
            CancellationToken transactionCancellationToken)
        {
            _source = source;
            _connection = connection;
            _transaction = transaction;
            _expectedParameterFamily = expectedParameterFamily;
            _transactionCancellationToken = transactionCancellationToken;
        }

        public Task<TSourceParams> ExecuteReaderAsync<TSourceParams>(
            TSourceParams @params,
            CancellationToken cancellationToken = default)
            where TSourceParams : BaseDatabaseSourceParams =>
            ExecuteSerializedAsync(
                @params,
                async ct => (TSourceParams)await _source.ExecuteTransactionReaderAsync(
                    @params, _connection, _transaction, ct).ConfigureAwait(false),
                cancellationToken);

        public Task<TSourceParams> ExecuteReaderAsync<TValue, TSourceParams>(
            TSourceParams @params,
            CancellationToken cancellationToken = default)
            where TValue : class, new()
            where TSourceParams : BaseDatabaseSourceParams<TValue> =>
            ExecuteSerializedAsync(
                @params,
                ct => _source.ExecuteTransactionReaderAsync<TValue, TSourceParams>(@params, _connection, _transaction, ct),
                cancellationToken);

        public Task<TSourceParams> ExecuteNonQueryAsync<TSourceParams>(
            TSourceParams @params,
            CancellationToken cancellationToken = default)
            where TSourceParams : BaseDatabaseSourceParams =>
            ExecuteSerializedAsync(
                @params,
                async ct => (TSourceParams)await _source.ExecuteTransactionNonQueryAsync(
                    @params, _connection, _transaction, ct).ConfigureAwait(false),
                cancellationToken);

        public Task<TSourceParams> ExecuteScalarAsync<TSourceParams>(
            TSourceParams @params,
            CancellationToken cancellationToken = default)
            where TSourceParams : BaseDatabaseSourceParams =>
            ExecuteSerializedAsync(
                @params,
                async ct => (TSourceParams)await _source.ExecuteTransactionScalarAsync(
                    @params, _connection, _transaction, ct).ConfigureAwait(false),
                cancellationToken);

        internal async Task DeactivateAndDrainAsync()
        {
            Interlocked.Exchange(ref _active, 0);
            await _commandGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            _commandGate.Release();
        }

        private async Task<TResult> ExecuteSerializedAsync<TParams, TResult>(
            TParams @params,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(@params);
            EnsureCompatibleParameterFamily(@params.GetType());

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                _transactionCancellationToken,
                cancellationToken);

            await _commandGate.WaitAsync(linkedCancellation.Token).ConfigureAwait(false);
            try
            {
                EnsureActive();
                return await operation(linkedCancellation.Token).ConfigureAwait(false);
            }
            finally
            {
                _commandGate.Release();
            }
        }

        private void EnsureActive()
        {
            if (Volatile.Read(ref _active) == 0)
            {
                throw new ObjectDisposedException(
                    nameof(IDatabaseTransaction),
                    "The transaction executor cannot be used after its callback has completed.");
            }
        }

        private void EnsureCompatibleParameterFamily(Type parameterType)
        {
            var actualFamily = parameterType.GetCleanGenericTypeName();
            if (!string.Equals(actualFamily, _expectedParameterFamily, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Parameters of type '{parameterType.Name}' cannot be used in a transaction for " +
                    $"'{_expectedParameterFamily}'.",
                    "params");
            }
        }
    }
}
