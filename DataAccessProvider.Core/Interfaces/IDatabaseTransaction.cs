using System.Data;
using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Core.Interfaces;

/// <summary>
/// Executes database commands on the connection and transaction owned by the
/// current <see cref="IDatabaseTransactionProvider{TSourceParams}"/> callback.
/// </summary>
/// <remarks>
/// Instances are valid only for the lifetime of their callback. Commands are
/// serialized because ADO.NET connections and transactions do not support
/// concurrent command execution reliably.
/// </remarks>
public interface IDatabaseTransaction
{
    Task<TSourceParams> ExecuteReaderAsync<TSourceParams>(
        TSourceParams @params,
        CancellationToken cancellationToken = default)
        where TSourceParams : BaseDatabaseSourceParams;

    Task<TSourceParams> ExecuteReaderAsync<TValue, TSourceParams>(
        TSourceParams @params,
        CancellationToken cancellationToken = default)
        where TValue : class, new()
        where TSourceParams : BaseDatabaseSourceParams<TValue>;

    Task<TSourceParams> ExecuteNonQueryAsync<TSourceParams>(
        TSourceParams @params,
        CancellationToken cancellationToken = default)
        where TSourceParams : BaseDatabaseSourceParams;

    Task<TSourceParams> ExecuteScalarAsync<TSourceParams>(
        TSourceParams @params,
        CancellationToken cancellationToken = default)
        where TSourceParams : BaseDatabaseSourceParams;
}

/// <summary>
/// Runs a callback in a managed local database transaction for a single data source.
/// </summary>
public interface IDatabaseTransactionProvider<TSourceParams>
    where TSourceParams : BaseDatabaseSourceParams
{
    Task ExecuteInTransactionAsync(
        Func<IDatabaseTransaction, CancellationToken, Task> operation,
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IDatabaseTransaction, CancellationToken, Task<TResult>> operation,
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default);
}
