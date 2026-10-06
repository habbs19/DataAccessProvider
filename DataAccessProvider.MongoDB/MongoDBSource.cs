using DataAccessProvider.Core;
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DataAccessProvider.MongoDB;

public sealed class MongoDBSource : BaseSource, IDataSource, ICancellableDataSource, IDataSource<MongoDBParams>, IDisposable
{
    private readonly string _connectionString;
    private readonly IMongoClient _client;
    private readonly ResourceOwnership _ownership;
    private int _disposed;
    public MongoDBSource(string connectionString, IResiliencePolicy? resiliencePolicy = null)
        : this(connectionString, new MongoClient(connectionString), ResourceOwnership.Owned, resiliencePolicy) { }
    public MongoDBSource(string connectionString, IMongoClient client, ResourceOwnership ownership = ResourceOwnership.Borrowed, IResiliencePolicy? resiliencePolicy = null)
    { _connectionString = connectionString; _client = client ?? throw new ArgumentNullException(nameof(client)); _ownership = ownership; }
    internal IMongoDatabase Database(string? name = null)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        name ??= MongoUrl.Create(_connectionString).DatabaseName;
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Specify a database in the connection URI or parameters.");
        return _client.GetDatabase(name);
    }
    public void Dispose() { if (Interlocked.Exchange(ref _disposed, 1) == 0 && _ownership == ResourceOwnership.Owned) _client.Dispose(); }
    protected override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams p) => Read(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> Read(BaseDataSourceParams p, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (p is not MongoDBParams request) throw new ArgumentException("Expected MongoDBParams.", nameof(p));
        var collection = Database(request.DatabaseName).GetCollection<BsonDocument>(request.CollectionName);
        List<BsonDocument> documents;
        if (request.OperationType == MongoOperationType.Find)
        {
            var options = new FindOptions<BsonDocument> { Sort = request.Sort, Skip = request.Skip, Limit = request.Limit };
            if (request.Projection is not null) options.Projection = request.Projection;
            using var cursor = await collection.FindAsync(request.Filter ?? Builders<BsonDocument>.Filter.Empty, options, ct).ConfigureAwait(false);
            documents = await cursor.ToListAsync(ct).ConfigureAwait(false);
        }
        else if (request.OperationType == MongoOperationType.Aggregate)
        {
            ArgumentNullException.ThrowIfNull(request.Pipeline);
            using var cursor = await collection.AggregateAsync(request.Pipeline, cancellationToken: ct).ConfigureAwait(false);
            documents = await cursor.ToListAsync(ct).ConfigureAwait(false);
        }
        else throw new ArgumentException("Reader requires Find or Aggregate.", nameof(p));
        var rows = documents.Select(BsonDocumentToDictionary).ToList();
        request.SetValue(rows.Count switch { 0 => (object)new Dictionary<string, object>(), 1 => rows[0], _ => rows });
        return request;
    }
    private async Task<MongoDBParams<TValue>> ReadTyped<TValue>(MongoDBParams<TValue> request, CancellationToken ct) where TValue : class
    {
        ct.ThrowIfCancellationRequested();
        var collection = Database(request.DatabaseName).GetCollection<TValue>(request.CollectionName);
        List<TValue> rows;
        if (request.OperationType == MongoOperationType.Find)
        {
            var options = new FindOptions<TValue> { Sort = request.Sort, Skip = request.Skip, Limit = request.Limit };
            if (request.Projection is not null) options.Projection = request.Projection;
            using var cursor = await collection.FindAsync(request.Filter ?? Builders<TValue>.Filter.Empty, options, ct).ConfigureAwait(false);
            rows = await cursor.ToListAsync(ct).ConfigureAwait(false);
        }
        else if (request.OperationType == MongoOperationType.Aggregate)
        {
            ArgumentNullException.ThrowIfNull(request.Pipeline);
            using var cursor = await collection.AggregateAsync(request.Pipeline, cancellationToken: ct).ConfigureAwait(false);
            rows = await cursor.ToListAsync(ct).ConfigureAwait(false);
        }
        else throw new ArgumentException("Reader requires Find or Aggregate.", nameof(request));
        request.SetValue(rows); return request;
    }
    protected override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams p)
        => ExecuteReaderAsync<TValue>(p,CancellationToken.None);
    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams p,CancellationToken ct) where TValue:class,new()
    {
        if (p is not MongoDBParams request) throw new ArgumentException("Expected MongoDBParams.", nameof(p));
        var result = await ReadTyped(Adapt<TValue>(request), ct).ConfigureAwait(false);
        request.SetValue(result.Value!.ToList()); return result;
    }
    private static MongoDBParams<TValue> Adapt<TValue>(MongoDBParams p) where TValue : class
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
        return new()
        {
            CollectionName = p.CollectionName,
            DatabaseName = p.DatabaseName,
            OperationType = p.OperationType,
            Skip = p.Skip,
            Limit = p.Limit,
            Filter = p.Filter is null ? null : new BsonDocumentFilterDefinition<TValue>(p.Filter.Render(new(serializer, BsonSerializer.SerializerRegistry))),
            Projection = p.Projection is null ? null : new BsonDocumentProjectionDefinition<TValue>(p.Projection.Render(new(serializer, BsonSerializer.SerializerRegistry))),
            Sort = p.Sort is null ? null : new BsonDocumentSortDefinition<TValue>(p.Sort.Render(new(serializer, BsonSerializer.SerializerRegistry))),
            Pipeline = p.Pipeline is null ? null : PipelineDefinition<TValue, TValue>.Create(p.Pipeline.Render(new(serializer, BsonSerializer.SerializerRegistry)).Documents)
        };
    }
    protected override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams p) => Write(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> Write(BaseDataSourceParams p, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (p is not MongoDBParams request) throw new ArgumentException("Expected MongoDBParams.", nameof(p));
        var collection = Database(request.DatabaseName).GetCollection<BsonDocument>(request.CollectionName);
        long count;
        switch (request.OperationType)
        {
            case MongoOperationType.InsertOne: ArgumentNullException.ThrowIfNull(request.Document); await collection.InsertOneAsync(request.Document, cancellationToken: ct); count = 1; break;
            case MongoOperationType.InsertMany: ArgumentNullException.ThrowIfNull(request.Documents); await collection.InsertManyAsync(request.Documents, cancellationToken: ct); count = request.Documents.Count; break;
            case MongoOperationType.UpdateOne: ArgumentNullException.ThrowIfNull(request.Filter); ArgumentNullException.ThrowIfNull(request.Update); count = (await collection.UpdateOneAsync(request.Filter, request.Update, cancellationToken: ct)).ModifiedCount; break;
            case MongoOperationType.UpdateMany: ArgumentNullException.ThrowIfNull(request.Filter); ArgumentNullException.ThrowIfNull(request.Update); count = (await collection.UpdateManyAsync(request.Filter, request.Update, cancellationToken: ct)).ModifiedCount; break;
            case MongoOperationType.DeleteOne: ArgumentNullException.ThrowIfNull(request.Filter); count = (await collection.DeleteOneAsync(request.Filter, ct)).DeletedCount; break;
            case MongoOperationType.DeleteMany: ArgumentNullException.ThrowIfNull(request.Filter); count = (await collection.DeleteManyAsync(request.Filter, ct)).DeletedCount; break;
            default: throw new ArgumentException("A write operation is required.", nameof(p));
        }
        request.SetValue(count); return request;
    }
    public async Task<MongoDBParams<TValue>> ExecuteNonQueryAsync<TValue>(MongoDBParams<TValue> request, CancellationToken cancellationToken = default) where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        var collection = Database(request.DatabaseName).GetCollection<TValue>(request.CollectionName);
        switch (request.OperationType)
        {
            case MongoOperationType.InsertOne: ArgumentNullException.ThrowIfNull(request.Document); await collection.InsertOneAsync(request.Document, cancellationToken: cancellationToken); break;
            case MongoOperationType.InsertMany: ArgumentNullException.ThrowIfNull(request.Documents); await collection.InsertManyAsync(request.Documents, cancellationToken: cancellationToken); break;
            case MongoOperationType.UpdateOne: ArgumentNullException.ThrowIfNull(request.Filter); ArgumentNullException.ThrowIfNull(request.Update); await collection.UpdateOneAsync(request.Filter, request.Update, cancellationToken: cancellationToken); break;
            case MongoOperationType.UpdateMany: ArgumentNullException.ThrowIfNull(request.Filter); ArgumentNullException.ThrowIfNull(request.Update); await collection.UpdateManyAsync(request.Filter, request.Update, cancellationToken: cancellationToken); break;
            case MongoOperationType.DeleteOne: ArgumentNullException.ThrowIfNull(request.Filter); await collection.DeleteOneAsync(request.Filter, cancellationToken); break;
            case MongoOperationType.DeleteMany: ArgumentNullException.ThrowIfNull(request.Filter); await collection.DeleteManyAsync(request.Filter, cancellationToken); break;
            default: throw new ArgumentException("A write operation is required.", nameof(request));
        }
        return request;
    }
    protected override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams p) => Scalar(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> Scalar(BaseDataSourceParams p, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (p is not MongoDBParams request) throw new ArgumentException("Expected MongoDBParams.", nameof(p));
        var collection = Database(request.DatabaseName).GetCollection<BsonDocument>(request.CollectionName);
        if (request.OperationType == MongoOperationType.Count) request.SetValue(await collection.CountDocumentsAsync(request.Filter ?? Builders<BsonDocument>.Filter.Empty, cancellationToken: ct));
        else if (request.OperationType == MongoOperationType.Aggregate)
        {
            ArgumentNullException.ThrowIfNull(request.Pipeline);
            using var cursor = await collection.AggregateAsync(request.Pipeline, cancellationToken: ct);
            var first = await cursor.FirstOrDefaultAsync(ct);
            request.SetValue(first is { ElementCount: > 0 } ? BsonTypeMapper.MapToDotNetValue(first.GetElement(0).Value) : null!);
        }
        else throw new ArgumentException("Scalar requires Count or Aggregate.", nameof(p));
        return request;
    }
    public Task<bool> CheckHealthAsync() => Health(null, CancellationToken.None);
    public Task<bool> CheckHealthAsync(CancellationToken ct) => Health(null, ct);
    public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => Health((p as MongoDBParams)?.DatabaseName, ct);
    public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => Health((p as MongoDBParams)?.DatabaseName, CancellationToken.None);
    private async Task<bool> Health(string? database, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        try { await Database(database).RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct); return true; }
        catch (OperationCanceledException) { throw; }
        catch when (!ct.IsCancellationRequested) { return false; }
    }
    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteReaderAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await Read(p, ct);
    public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteNonQueryAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await Write(p, ct);
    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteScalarAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await Scalar(p, ct);
    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams p) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue> => ExecuteReaderAsync<TValue, TBaseDataSourceParams>(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue>
    {
        if (p is not MongoDBParams<TValue> request) throw new ArgumentException("Expected typed MongoDBParams.", nameof(p));
        await ReadTyped(request, ct); return p;
    }
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(BaseDataSourceParams<TValue> p) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        => (TBaseDataSourceParams)await ExecuteReaderAsync<TValue>(p);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> p) where TValue : class, new() => ExecuteReaderAsync(p, CancellationToken.None);
    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> p, CancellationToken ct) where TValue : class, new()
    { if (p is not MongoDBParams<TValue> request) throw new ArgumentException("Expected typed MongoDBParams.", nameof(p)); return await ReadTyped(request, ct); }
    public async Task<MongoDBParams> ExecuteReaderAsync(MongoDBParams p) => (MongoDBParams)await Read(p, CancellationToken.None);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(MongoDBParams p) where TValue : class, new() => ExecuteReader<TValue>(p);
    public async Task<MongoDBParams> ExecuteNonQueryAsync(MongoDBParams p) => (MongoDBParams)await Write(p, CancellationToken.None);
    public async Task<MongoDBParams> ExecuteScalarAsync(MongoDBParams p) => (MongoDBParams)await Scalar(p, CancellationToken.None);
    private static Dictionary<string, object> BsonDocumentToDictionary(BsonDocument document)
        => document.Elements.ToDictionary(x => x.Name, x => BsonTypeMapper.MapToDotNetValue(x.Value)!);
}
