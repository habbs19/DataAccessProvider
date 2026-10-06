using System.Diagnostics;
using System.Linq.Expressions;
using DataAccessProvider.Core;
using MongoDB.Bson;
using MongoDB.Driver;
namespace DataAccessProvider.MongoDB;

public sealed class MongoDocumentClient : IDocumentClient
{
    public Task<IReadOnlyList<T>> FindAsync<T>(string collection, Expression<Func<T, bool>>? filter = null, int? limit = null, int? skip = null, CancellationToken cancellationToken = default) where T : class
        => DataAccessDiagnostics.MeasureAsync("MongoDB", "document.find", () => FindCoreAsync(collection, filter, limit, skip, cancellationToken));
    public Task<long> CountAsync<T>(string collection, Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default) where T : class
        => DataAccessDiagnostics.MeasureAsync("MongoDB", "document.count", () => CountCoreAsync(collection, filter, cancellationToken));
    public Task<DocumentWriteResult> InsertAsync<T>(string collection, T document, CancellationToken cancellationToken = default) where T : class
        => DataAccessDiagnostics.MeasureAsync("MongoDB", "document.insert", () => InsertCoreAsync(collection, document, cancellationToken));
    public Task<DocumentWriteResult> InsertManyAsync<T>(string collection, IReadOnlyCollection<T> documents, CancellationToken cancellationToken = default) where T : class
        => DataAccessDiagnostics.MeasureAsync("MongoDB", "document.insert_many", () => InsertManyCoreAsync(collection, documents, cancellationToken));
    public Task<DocumentWriteResult> ReplaceAsync<T>(string collection, Expression<Func<T, bool>> filter, T document, bool upsert = false, CancellationToken cancellationToken = default) where T : class
        => DataAccessDiagnostics.MeasureAsync("MongoDB", "document.replace", () => ReplaceCoreAsync(collection, filter, document, upsert, cancellationToken));
    public Task<DocumentWriteResult> SetAsync<T, TValue>(string collection, Expression<Func<T, bool>> filter, Expression<Func<T, TValue>> field, TValue value, bool many = false, CancellationToken cancellationToken = default) where T : class
        => DataAccessDiagnostics.MeasureAsync("MongoDB", "document.update", () => SetCoreAsync(collection, filter, field, value, many, cancellationToken));
    public Task<DocumentWriteResult> DeleteAsync<T>(string collection, Expression<Func<T, bool>> filter, bool many = false, CancellationToken cancellationToken = default) where T : class
        => DataAccessDiagnostics.MeasureAsync("MongoDB", "document.delete", () => DeleteCoreAsync(collection, filter, many, cancellationToken));
    private readonly MongoDBSource _source;
    public MongoDocumentClient(string connectionString) : this(connectionString, new MongoClient(connectionString), ResourceOwnership.Owned) { }
    public MongoDocumentClient(string connectionString, IMongoClient client, ResourceOwnership ownership = ResourceOwnership.Borrowed)
        => _source = new MongoDBSource(connectionString, client, ownership);
    private IMongoCollection<T> Collection<T>(string name, CancellationToken ct) where T : class
    { ct.ThrowIfCancellationRequested(); ArgumentException.ThrowIfNullOrWhiteSpace(name); return _source.Database().GetCollection<T>(name); }
    private async Task<IReadOnlyList<T>> FindCoreAsync<T>(string collection, Expression<Func<T, bool>>? filter = null, int? limit = null, int? skip = null, CancellationToken cancellationToken = default) where T : class
    {
        if (limit < 0 || skip < 0) throw new ArgumentOutOfRangeException(nameof(limit));
        using var cursor = await Collection<T>(collection, cancellationToken).FindAsync(filter is null ? Builders<T>.Filter.Empty : new ExpressionFilterDefinition<T>(filter),
            new FindOptions<T> { Limit = limit, Skip = skip }, cancellationToken);
        return (await cursor.ToListAsync(cancellationToken)).AsReadOnly();
    }
    private Task<long> CountCoreAsync<T>(string collection, Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default) where T : class
        => Collection<T>(collection, cancellationToken).CountDocumentsAsync(filter is null ? Builders<T>.Filter.Empty : new ExpressionFilterDefinition<T>(filter), cancellationToken: cancellationToken);
    private async Task<DocumentWriteResult> InsertCoreAsync<T>(string collection, T document, CancellationToken cancellationToken = default) where T : class
    { ArgumentNullException.ThrowIfNull(document); await Collection<T>(collection, cancellationToken).InsertOneAsync(document, cancellationToken: cancellationToken); return new(Inserted: 1); }
    private async Task<DocumentWriteResult> InsertManyCoreAsync<T>(string collection, IReadOnlyCollection<T> documents, CancellationToken cancellationToken = default) where T : class
    { ArgumentNullException.ThrowIfNull(documents); await Collection<T>(collection, cancellationToken).InsertManyAsync(documents, cancellationToken: cancellationToken); return new(Inserted: documents.Count); }
    private async Task<DocumentWriteResult> ReplaceCoreAsync<T>(string collection, Expression<Func<T, bool>> filter, T document, bool upsert = false, CancellationToken cancellationToken = default) where T : class
    { var result = await Collection<T>(collection, cancellationToken).ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = upsert }, cancellationToken); return new(Matched: result.MatchedCount, Modified: result.ModifiedCount, Upserted: result.UpsertedId is not null); }
    private async Task<DocumentWriteResult> SetCoreAsync<T, TValue>(string collection, Expression<Func<T, bool>> filter, Expression<Func<T, TValue>> field, TValue value, bool many = false, CancellationToken cancellationToken = default) where T : class
    {
        var target = Collection<T>(collection, cancellationToken); var update = Builders<T>.Update.Set(field, value);
        var result = many ? await target.UpdateManyAsync(filter, update, cancellationToken: cancellationToken) : await target.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return new(Matched: result.MatchedCount, Modified: result.ModifiedCount);
    }
    private async Task<DocumentWriteResult> DeleteCoreAsync<T>(string collection, Expression<Func<T, bool>> filter, bool many = false, CancellationToken cancellationToken = default) where T : class
    { var target = Collection<T>(collection, cancellationToken); var result = many ? await target.DeleteManyAsync(filter, cancellationToken) : await target.DeleteOneAsync(filter, cancellationToken); return new(Deleted: result.DeletedCount); }
    public async Task<HealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var start = Stopwatch.GetTimestamp();
        try { await _source.Database().RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken); return new(true, HealthFailure.None, null, Stopwatch.GetElapsedTime(start)); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        { return new(false, ex is MongoAuthenticationException ? HealthFailure.Authentication : ex is MongoConnectionException ? HealthFailure.Connectivity : ex is ArgumentException or InvalidOperationException ? HealthFailure.Configuration : HealthFailure.Unknown, null, Stopwatch.GetElapsedTime(start)); }
    }
    /// <summary>Native sessions, aggregation and driver-specific capabilities. Cursor/session ownership belongs to the callback.</summary>
    public Task<T> ExecuteNativeAsync<T>(Func<IMongoDatabase, CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); ArgumentNullException.ThrowIfNull(action); return action(_source.Database(), cancellationToken); }
    public ValueTask DisposeAsync() { _source.Dispose(); return ValueTask.CompletedTask; }
}
