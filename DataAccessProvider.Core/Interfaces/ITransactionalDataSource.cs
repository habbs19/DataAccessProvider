using System.Data;
using System.Data.Common;
using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Core.Interfaces;

/// <summary>
/// Represents a data source that supports transactional operations.
/// </summary>
public interface ITransactionalDataSource : IDataSource
{
    /// <summary>
    /// Begins a new database transaction with the default isolation level.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A transaction context that can be used to execute operations within the transaction.</returns>
    Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction with the specified isolation level.
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A transaction context that can be used to execute operations within the transaction.</returns>
    Task<ITransactionContext> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a transaction context that manages the lifecycle of a database transaction.
/// Implements IAsyncDisposable for proper cleanup in async scenarios.
/// </summary>
public interface ITransactionContext : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// Gets the underlying database connection for this transaction.
    /// </summary>
    DbConnection Connection { get; }

    /// <summary>
    /// Gets the underlying database transaction.
    /// </summary>
    DbTransaction Transaction { get; }

    /// <summary>
    /// Gets whether the transaction has been committed.
    /// </summary>
    bool IsCommitted { get; }

    /// <summary>
    /// Gets whether the transaction has been rolled back.
    /// </summary>
    bool IsRolledBack { get; }

    /// <summary>
    /// Gets whether the transaction is still active (not committed or rolled back).
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Executes a query within this transaction and maps results to a list of objects.
    /// </summary>
    Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        where TValue : class, new();

    /// <summary>
    /// Executes a query within this transaction and returns raw dictionary results.
    /// </summary>
    Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams;

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE) within this transaction.
    /// </summary>
    Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams;

    /// <summary>
    /// Executes a scalar command within this transaction and returns a single value.
    /// </summary>
    Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams;

    /// <summary>
    /// Commits the transaction, making all changes permanent.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction, undoing all changes.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
