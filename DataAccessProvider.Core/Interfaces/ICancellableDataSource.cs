using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Core.Interfaces;

/// <summary>Additive cancellation contract for legacy requests; existing custom sources remain compatible.</summary>
public interface ICancellableDataSource
{
    Task<TParams> ExecuteReaderAsync<TParams>(TParams parameters, CancellationToken cancellationToken) where TParams : BaseDataSourceParams;
    Task<TParams> ExecuteScalarAsync<TParams>(TParams parameters, CancellationToken cancellationToken) where TParams : BaseDataSourceParams;
    Task<TParams> ExecuteNonQueryAsync<TParams>(TParams parameters, CancellationToken cancellationToken) where TParams : BaseDataSourceParams;
    Task<TParams> ExecuteReaderAsync<T, TParams>(TParams parameters, CancellationToken cancellationToken) where T : class, new() where TParams : BaseDataSourceParams<T>;
    Task<BaseDataSourceParams<T>> ExecuteReaderAsync<T>(BaseDataSourceParams<T> parameters, CancellationToken cancellationToken) where T : class, new();
    Task<BaseDataSourceParams<T>> ExecuteReaderAsync<T>(BaseDataSourceParams parameters, CancellationToken cancellationToken) where T : class, new();
    Task<bool> CheckHealthAsync<TParams>(TParams parameters, CancellationToken cancellationToken) where TParams : BaseDataSourceParams;
}
