using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core;

public class DatabaseClient<TProvider> : IDatabaseClient<TProvider>
{
    private readonly IRelationalProvider<TProvider> _provider;
    private readonly ResourceOwnership _ownership;
    private readonly DatabaseClientOptions _options;
    private int _disposed;
    private static ActivitySource Activities => DataAccessDiagnostics.Activities;
    private static Histogram<double> Duration => DataAccessDiagnostics.Duration;
    private static Counter<long> Attempts => DataAccessDiagnostics.Attempts;

    public DatabaseClient(IRelationalProvider<TProvider> provider, DatabaseClientOptions? options = null,
        ResourceOwnership ownership = ResourceOwnership.Borrowed)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider; _ownership = ownership;
        options ??= new();
        ArgumentNullException.ThrowIfNull(options.DelayAsync);
        if (options.MaxRetries < 0 || options.InitialDelay < TimeSpan.Zero || options.MaxDelay < options.InitialDelay)
            throw new ArgumentOutOfRangeException(nameof(options));
        _options = new()
        {
            MaxRetries = options.MaxRetries,
            InitialDelay = options.InitialDelay,
            MaxDelay = options.MaxDelay,
            IsTransient = options.IsTransient,
            DelayAsync = options.DelayAsync
        };
    }

    public Task<QueryResult<T>> QueryAsync<T>(DatabaseCommand command, CancellationToken cancellationToken = default) where T : class, new()
        => Run(command, (connection, transaction, ct) => QueryCore(command, connection, transaction,
            reader => RowMapper<T>.Create(reader), ct), cancellationToken);
    public Task<QueryResult<T>> QueryAsync<T>(DatabaseCommand command, Func<DbDataReader, T> mapper, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return Run(command, (connection, transaction, ct) => QueryCore(command, connection, transaction, _ => mapper, ct), cancellationToken);
    }
    internal Task<QueryResult<T>> QueryLegacyAsync<T>(DatabaseCommand command, CancellationToken ct) where T : class, new()
        => Run(command, (connection, transaction, token) => QueryCore(command, connection, transaction,
            reader => RowMapper<T>.Create(reader, strict: false), token), ct);

    /// <summary>Streams the first result set. Enumeration owns resources; dispose it on early exit. No retries or outputs.</summary>
    public IAsyncEnumerable<T> StreamAsync<T>(DatabaseCommand command, CancellationToken cancellationToken = default) where T : class, new()
        => StreamCore(command, reader => RowMapper<T>.Create(reader), cancellationToken);
    public IAsyncEnumerable<T> StreamAsync<T>(DatabaseCommand command, Func<DbDataReader, T> mapper, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return StreamCore(command, _ => mapper, cancellationToken);
    }
    private async IAsyncEnumerable<T> StreamCore<T>(DatabaseCommand definition, Func<DbDataReader, Func<DbDataReader, T>> mapperFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Validate(definition);
        if (definition.Parameters.Any(p => p.Direction != DataAccessParameterDirection.Input))
            throw new NotSupportedException("Use buffered queries when output parameters are required.");
        cancellationToken.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (definition.Timeout != Timeout.InfiniteTimeSpan) timeout.CancelAfter(definition.Timeout);
        using var activity = Activities.StartActivity("database.stream");
        activity?.SetTag("db.provider", typeof(TProvider).Name);
        var started = Stopwatch.GetTimestamp();
        try
        {
            Attempts.Add(1, new KeyValuePair<string, object?>("db.provider", typeof(TProvider).Name));
            await using var connection = _provider.CreateConnection();
            await connection.OpenAsync(timeout.Token).ConfigureAwait(false);
            await using var command = Create(definition, connection, null);
            await using var reader = await command.ExecuteReaderAsync(timeout.Token).ConfigureAwait(false);
            var map = mapperFactory(reader);
            while (await reader.ReadAsync(timeout.Token).ConfigureAwait(false))
            {
                timeout.Token.ThrowIfCancellationRequested();
                yield return map(reader);
            }
            timeout.Token.ThrowIfCancellationRequested();
        }
        finally { Duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, new KeyValuePair<string, object?>("db.provider", typeof(TProvider).Name)); }
    }
    public Task<ScalarResult<T>> ScalarAsync<T>(DatabaseCommand command, CancellationToken cancellationToken = default)
        => Run(command, (connection, transaction, ct) => ScalarCore<T>(command, connection, transaction, ct), cancellationToken);
    public Task<CommandResult> ExecuteAsync(DatabaseCommand command, CancellationToken cancellationToken = default)
        => Run(command, (connection, transaction, ct) => ExecuteCore(command, connection, transaction, ct), cancellationToken);
    public Task<MultipleResult> QueryMultipleAsync(DatabaseCommand command, CancellationToken cancellationToken = default)
        => Run(command, (connection, transaction, ct) => MultipleCore(command, connection, transaction, ct, true), cancellationToken);
    internal Task<MultipleResult> QueryMultipleLegacyAsync(DatabaseCommand command, CancellationToken ct)
        => Run(command, (connection, transaction, token) => MultipleCore(command, connection, transaction, token, false), ct);

    private async Task<TResult> Run<TResult>(DatabaseCommand definition,
        Func<DbConnection, DbTransaction?, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken)
    {
        Validate(definition); cancellationToken.ThrowIfCancellationRequested();
        using var activity = Activities.StartActivity("database.operation");
        activity?.SetTag("db.provider", typeof(TProvider).Name);
        var started = Stopwatch.GetTimestamp();
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (definition.Timeout != Timeout.InfiniteTimeSpan) timeout.CancelAfter(definition.Timeout);
                try
                {
                    Attempts.Add(1, new KeyValuePair<string, object?>("db.provider", typeof(TProvider).Name));
                    await using var connection = _provider.CreateConnection();
                    await connection.OpenAsync(timeout.Token).ConfigureAwait(false);
                    var result = await action(connection, null, timeout.Token).ConfigureAwait(false);
                    timeout.Token.ThrowIfCancellationRequested();
                    return result;
                }
                catch (Exception ex) when (definition.RetrySafety != RetrySafety.None && attempt < _options.MaxRetries &&
                    !timeout.IsCancellationRequested && ex is not (OperationCanceledException or ArgumentException or MappingException or NotSupportedException or FormatException) &&
                    (_options.IsTransient?.Invoke(ex) ?? _provider.IsTransient(ex)))
                {
                    var milliseconds = Math.Min(_options.MaxDelay.TotalMilliseconds,
                        _options.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt)) * (0.8 + Random.Shared.NextDouble() * 0.2);
                    await _options.DelayAsync(TimeSpan.FromMilliseconds(milliseconds), cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch { activity?.SetStatus(ActivityStatusCode.Error); throw; }
        finally { Duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, new KeyValuePair<string, object?>("db.provider", typeof(TProvider).Name)); }
    }

    private void Validate(DatabaseCommand command)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentNullException.ThrowIfNull(command);
        if (command.CommandType == CommandType.StoredProcedure && !_provider.Capabilities.StoredProcedures)
            throw new NotSupportedException("This provider requires a text CALL command instead of CommandType.StoredProcedure.");
        if (!_provider.Capabilities.OutputParameters && command.Parameters.Any(p => p.Direction != DataAccessParameterDirection.Input))
            throw new NotSupportedException("This provider does not expose ADO.NET output parameters.");
        _provider.Validate(command);
    }
    private DbCommand Create(DatabaseCommand definition, DbConnection connection, DbTransaction? transaction)
    {
        var command = _provider.CreateCommand(connection, definition);
        try
        {
            command.CommandText = definition.CommandText; command.CommandType = definition.CommandType;
            command.CommandTimeout = definition.Timeout == Timeout.InfiniteTimeSpan ? 0 : checked((int)Math.Ceiling(definition.Timeout.TotalSeconds));
            command.Transaction = transaction;
            foreach (var parameter in definition.Parameters) command.Parameters.Add(_provider.CreateParameter(command, parameter));
            return command;
        }
        catch { command.Dispose(); throw; }
    }
    private (IReadOnlyDictionary<string, object?> Outputs, object? ReturnValue) Outputs(DatabaseCommand definition, DbCommand command)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal); object? returnValue = null;
        for (var index = 0; index < definition.Parameters.Count; index++)
        {
            var parameter = definition.Parameters[index];
            if (parameter.Direction == DataAccessParameterDirection.Input) continue;
            var value = _provider.ReadOutput((DbParameter)command.Parameters[index]);
            if (parameter.Direction == DataAccessParameterDirection.ReturnValue) returnValue = value;
            else values[parameter.Name] = value;
        }
        return (new ReadOnlyDictionary<string, object?>(values), returnValue);
    }
    private async Task<QueryResult<T>> QueryCore<T>(DatabaseCommand definition, DbConnection connection, DbTransaction? transaction,
        Func<DbDataReader, Func<DbDataReader, T>> mapperFactory, CancellationToken ct)
    {
        await using var command = Create(definition, connection, transaction);
        var rows = new List<T>();
        await using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            var map = mapperFactory(reader);
            while (await reader.ReadAsync(ct).ConfigureAwait(false)) rows.Add(map(reader));
            // Drain remaining result sets before output values become available.
            while (await reader.NextResultAsync(ct).ConfigureAwait(false))
                while (await reader.ReadAsync(ct).ConfigureAwait(false)) { }
        }
        var outputs = Outputs(definition, command);
        return new(rows.AsReadOnly(), outputs.Outputs, outputs.ReturnValue);
    }
    private async Task<ScalarResult<T>> ScalarCore<T>(DatabaseCommand definition, DbConnection connection, DbTransaction? transaction, CancellationToken ct)
    {
        await using var command = Create(definition, connection, transaction);
        var raw = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
        var state = raw is null ? ScalarState.NoRow : raw is DBNull ? ScalarState.Null : ScalarState.Value;
        var value = state == ScalarState.Value ? (T?)ValueConversion.ConvertValue(raw, typeof(T), "scalar") : default;
        var outputs = Outputs(definition, command);
        return new(state, value, outputs.Outputs, outputs.ReturnValue);
    }
    private async Task<CommandResult> ExecuteCore(DatabaseCommand definition, DbConnection connection, DbTransaction? transaction, CancellationToken ct)
    {
        await using var command = Create(definition, connection, transaction);
        var count = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        var outputs = Outputs(definition, command);
        return new(count, outputs.Outputs, outputs.ReturnValue);
    }
    private async Task<MultipleResult> MultipleCore(DatabaseCommand definition, DbConnection connection, DbTransaction? transaction, CancellationToken ct, bool strict)
    {
        await using var command = Create(definition, connection, transaction);
        var sets = new List<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        await using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
        {
            do
            {
                var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
                if (strict && names.Distinct(StringComparer.Ordinal).Count() != names.Length)
                    throw new InvalidOperationException("Duplicate result column names require explicit aliases or a custom mapper.");
                var rows = new List<IReadOnlyDictionary<string, object?>>();
                while (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var row = new Dictionary<string, object?>(StringComparer.Ordinal);
                    for (var i = 0; i < names.Length; i++) row[names[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rows.Add(new ReadOnlyDictionary<string, object?>(row));
                }
                sets.Add(rows.AsReadOnly());
            } while (await reader.NextResultAsync(ct).ConfigureAwait(false));
        }
        var outputs = Outputs(definition, command);
        return new(sets.AsReadOnly(), outputs.Outputs, outputs.ReturnValue);
    }
    public async Task<TResult> ExecuteNativeAsync<TResult>(Func<DbConnection, DbTransaction?, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentNullException.ThrowIfNull(action); cancellationToken.ThrowIfCancellationRequested();
        await using var connection = _provider.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var result = await action(connection, null, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }
    public Task ExecuteInTransactionAsync(Func<IDatabaseSession, CancellationToken, Task> action, IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        return ExecuteInTransactionAsync(async (session, ct) => { await action(session, ct).ConfigureAwait(false); return true; }, isolationLevel, cancellationToken);
    }
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<IDatabaseSession, CancellationToken, Task<TResult>> action, IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        ArgumentNullException.ThrowIfNull(action); cancellationToken.ThrowIfCancellationRequested();
        if (!_provider.Capabilities.Transactions) throw new NotSupportedException("Transactions are not supported by this provider.");
        if (isolationLevel.HasValue && _provider.Capabilities.IsolationLevels is { } supported && !supported.Contains(isolationLevel.Value))
            throw new NotSupportedException("This isolation level is not supported by the provider.");
        await using var connection = _provider.CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = isolationLevel.HasValue
            ? await connection.BeginTransactionAsync(isolationLevel.Value, cancellationToken).ConfigureAwait(false)
            : await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var session = new Session(this, connection, transaction, cancellationToken);
        TResult result;
        try
        {
            result = await action(session, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await session.CloseAsync().ConfigureAwait(false);
        }
        catch (Exception original)
        {
            await session.CloseAsync().ConfigureAwait(false);
            try { await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); }
            catch (Exception rollback) { throw new DatabaseTransactionException("Operation and rollback failed.", DatabaseTransactionFailureStage.Rollback, original, rollback, false); }
            throw;
        }
        try { await transaction.CommitAsync(cancellationToken).ConfigureAwait(false); return result; }
        catch (Exception commit)
        {
            Exception? rollback = null;
            try { await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false); } catch (Exception ex) { rollback = ex; }
            throw new DatabaseTransactionException("Commit failed; outcome is unknown. Do not replay automatically.", DatabaseTransactionFailureStage.Commit, commit, rollback, true);
        }
    }
    public async Task<HealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        cancellationToken.ThrowIfCancellationRequested(); var started = Stopwatch.GetTimestamp();
        try
        {
            await using var connection = _provider.CreateConnection(); await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return new(true, HealthFailure.None, null, Stopwatch.GetElapsedTime(started));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new(false, _provider.ClassifyHealthFailure(ex),
                ex is DbException db ? db.ErrorCode.ToString(System.Globalization.CultureInfo.InvariantCulture) : null, Stopwatch.GetElapsedTime(started));
        }
    }
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0 && _ownership == ResourceOwnership.Owned) await _provider.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class Session(DatabaseClient<TProvider> owner, DbConnection connection, DbTransaction transaction, CancellationToken transactionToken) : IDatabaseSession
    {
        private readonly SemaphoreSlim _gate = new(1);
        private int _active = 1;
        private async Task<TResult> Run<TResult>(DatabaseCommand? definition, Func<CancellationToken, Task<TResult>> action, CancellationToken ct)
        {
            if (definition is not null) owner.Validate(definition);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, transactionToken);
            if (definition is not null && definition.Timeout != Timeout.InfiniteTimeSpan) linked.CancelAfter(definition.Timeout);
            await _gate.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                if (Volatile.Read(ref _active) == 0) throw new InvalidOperationException("The transaction session has completed.");
                var result = await action(linked.Token).ConfigureAwait(false);
                linked.Token.ThrowIfCancellationRequested();
                return result;
            }
            finally { _gate.Release(); }
        }
        public async Task CloseAsync()
        {
            Interlocked.Exchange(ref _active, 0); await _gate.WaitAsync().ConfigureAwait(false); _gate.Release();
        }
        public Task<QueryResult<T>> QueryAsync<T>(DatabaseCommand command, CancellationToken cancellationToken = default) where T : class, new()
            => Run(command, ct => owner.QueryCore(command, connection, transaction, reader => RowMapper<T>.Create(reader), ct), cancellationToken);
        public Task<QueryResult<T>> QueryAsync<T>(DatabaseCommand command, Func<DbDataReader, T> mapper, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(mapper);
            return Run(command, ct => owner.QueryCore(command, connection, transaction, _ => mapper, ct), cancellationToken);
        }
        public Task<ScalarResult<T>> ScalarAsync<T>(DatabaseCommand command, CancellationToken cancellationToken = default)
            => Run(command, ct => owner.ScalarCore<T>(command, connection, transaction, ct), cancellationToken);
        public Task<CommandResult> ExecuteAsync(DatabaseCommand command, CancellationToken cancellationToken = default)
            => Run(command, ct => owner.ExecuteCore(command, connection, transaction, ct), cancellationToken);
        public Task<MultipleResult> QueryMultipleAsync(DatabaseCommand command, CancellationToken cancellationToken = default)
            => Run(command, ct => owner.MultipleCore(command, connection, transaction, ct, true), cancellationToken);
        public Task<TResult> ExecuteNativeAsync<TResult>(Func<DbConnection, DbTransaction?, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            return Run(null, ct => action(connection, transaction, ct), cancellationToken);
        }
    }
}
