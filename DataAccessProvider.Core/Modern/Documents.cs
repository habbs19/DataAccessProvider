using System.Linq.Expressions;
namespace DataAccessProvider.Core;

public sealed record DocumentWriteResult(long Inserted = 0, long Matched = 0, long Modified = 0, long Deleted = 0, bool Upserted = false);
public interface IDocumentClient : IAsyncDisposable
{
    Task<IReadOnlyList<T>> FindAsync<T>(string collection, Expression<Func<T, bool>>? filter = null, int? limit = null, int? skip = null, CancellationToken cancellationToken = default) where T : class;
    Task<long> CountAsync<T>(string collection, Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default) where T : class;
    Task<DocumentWriteResult> InsertAsync<T>(string collection, T document, CancellationToken cancellationToken = default) where T : class;
    Task<DocumentWriteResult> InsertManyAsync<T>(string collection, IReadOnlyCollection<T> documents, CancellationToken cancellationToken = default) where T : class;
    Task<DocumentWriteResult> ReplaceAsync<T>(string collection, Expression<Func<T, bool>> filter, T document, bool upsert = false, CancellationToken cancellationToken = default) where T : class;
    Task<DocumentWriteResult> SetAsync<T, TValue>(string collection, Expression<Func<T, bool>> filter, Expression<Func<T, TValue>> field, TValue value, bool many = false, CancellationToken cancellationToken = default) where T : class;
    Task<DocumentWriteResult> DeleteAsync<T>(string collection, Expression<Func<T, bool>> filter, bool many = false, CancellationToken cancellationToken = default) where T : class;
    Task<HealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
