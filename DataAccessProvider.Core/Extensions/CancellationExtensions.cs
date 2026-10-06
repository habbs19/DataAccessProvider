using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;

namespace DataAccessProvider.Core.Extensions;

public static class CancellationExtensions
{
    public static ICancellableDataSource WithCancellation<TParams>(this IDataSource<TParams> source) where TParams : BaseDataSourceParams
        => source as ICancellableDataSource ?? throw new NotSupportedException("This custom source must implement ICancellableDataSource to accept cancellation.");
    public static ICancellableDataSource WithCancellation(this IDataSource source)
        => source as ICancellableDataSource ?? throw new NotSupportedException("This custom source must implement ICancellableDataSource to accept cancellation.");
    public static Task<TParams> ExecuteReaderAsync<TParams>(this IDataSource source, TParams parameters, CancellationToken cancellationToken) where TParams : BaseDataSourceParams
        => source.WithCancellation().ExecuteReaderAsync(parameters, cancellationToken);
    public static Task<TParams> ExecuteScalarAsync<TParams>(this IDataSource source, TParams parameters, CancellationToken cancellationToken) where TParams : BaseDataSourceParams
        => source.WithCancellation().ExecuteScalarAsync(parameters, cancellationToken);
    public static Task<TParams> ExecuteNonQueryAsync<TParams>(this IDataSource source, TParams parameters, CancellationToken cancellationToken) where TParams : BaseDataSourceParams
        => source.WithCancellation().ExecuteNonQueryAsync(parameters, cancellationToken);
    public static Task<TParams> ExecuteReaderAsync<T, TParams>(this IDataSource source, TParams parameters, CancellationToken cancellationToken) where T : class, new() where TParams : BaseDataSourceParams<T>
        => source.WithCancellation().ExecuteReaderAsync<T, TParams>(parameters, cancellationToken);
    public static Task<BaseDataSourceParams<T>> ExecuteReaderAsync<T>(this IDataSource source, BaseDataSourceParams<T> parameters, CancellationToken cancellationToken) where T : class, new()
        => source.WithCancellation().ExecuteReaderAsync(parameters, cancellationToken);
    public static Task<BaseDataSourceParams<T>> ExecuteReaderAsync<T>(this IDataSource source, BaseDataSourceParams parameters, CancellationToken cancellationToken) where T : class, new()
        => source.WithCancellation().ExecuteReaderAsync<T>(parameters, cancellationToken);
    public static Task<bool> CheckHealthAsync<TParams>(this IDataSource source, TParams parameters, CancellationToken cancellationToken) where TParams : BaseDataSourceParams
        => source.WithCancellation().CheckHealthAsync(parameters, cancellationToken);
}
