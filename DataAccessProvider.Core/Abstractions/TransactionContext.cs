using System.Data;
using System.Data.Common;
using DataAccessProvider.Core.Interfaces;

namespace DataAccessProvider.Core.Abstractions;

/// <summary>
/// Manages the lifecycle of a database transaction, providing methods to execute
/// operations within the transaction and to commit or rollback changes.
/// </summary>
/// <typeparam name="TParameter">The type of database parameter (e.g., SqlParameter, NpgsqlParameter).</typeparam>
public class TransactionContext<TParameter> : ITransactionContext
    where TParameter : DbParameter
{
    private readonly BaseDatabaseSource<TParameter> _source;
    private readonly DbConnection _connection;
    private readonly DbTransaction _transaction;
    private bool _isCommitted;
    private bool _isRolledBack;
    private bool _isDisposed;

    /// <summary>
    /// Creates a new transaction context with an already opened connection and started transaction.
    /// </summary>
    internal TransactionContext(
        BaseDatabaseSource<TParameter> source,
        DbConnection connection,
        DbTransaction transaction)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <inheritdoc />
    public DbConnection Connection => _connection;

    /// <inheritdoc />
    public DbTransaction Transaction => _transaction;

    /// <inheritdoc />
    public bool IsCommitted => _isCommitted;

    /// <inheritdoc />
    public bool IsRolledBack => _isRolledBack;

    /// <inheritdoc />
    public bool IsActive => !_isCommitted && !_isRolledBack && !_isDisposed;

    /// <inheritdoc />
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        where TValue : class, new()
    {
        ThrowIfNotActive();
        return await _source.ExecuteReaderInTransactionAsync<TValue, TBaseDataSourceParams>(@params, _connection, _transaction)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        ThrowIfNotActive();
        return await _source.ExecuteReaderInTransactionAsync(@params, _connection, _transaction)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        ThrowIfNotActive();
        return await _source.ExecuteNonQueryInTransactionAsync(@params, _connection, _transaction)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        ThrowIfNotActive();
        return await _source.ExecuteScalarInTransactionAsync(@params, _connection, _transaction)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotActive();

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        _isCommitted = true;
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfNotActive();

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        _isRolledBack = true;
    }

    private void ThrowIfNotActive()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(TransactionContext<TParameter>));
        }

        if (_isCommitted)
        {
            throw new InvalidOperationException("Transaction has already been committed.");
        }

        if (_isRolledBack)
        {
            throw new InvalidOperationException("Transaction has already been rolled back.");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // If transaction is still active (not committed or rolled back), roll it back
        if (IsActive)
        {
            try
            {
                await _transaction.RollbackAsync().ConfigureAwait(false);
                _isRolledBack = true;
            }
            catch
            {
                // Swallow rollback exceptions during dispose
            }
        }

        await _transaction.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // If transaction is still active (not committed or rolled back), roll it back
        if (!_isCommitted && !_isRolledBack)
        {
            try
            {
                _transaction.Rollback();
                _isRolledBack = true;
            }
            catch
            {
                // Swallow rollback exceptions during dispose
            }
        }

        _transaction.Dispose();
        _connection.Dispose();
    }
}
