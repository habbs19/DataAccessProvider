using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Text.Json;

namespace DataAccessProvider.MongoDB;

public sealed class MongoDBSource : BaseSource, IDataSource, IDataSource<MongoDBParams>
{
    private readonly string _connectionString;
    private readonly IResiliencePolicy? _resiliencePolicy;
    private readonly MongoClient _client;

    public MongoDBSource(string connectionString, IResiliencePolicy? resiliencePolicy = null)
    {
        _connectionString = connectionString;
        _resiliencePolicy = resiliencePolicy;
        _client = new MongoClient(_connectionString);
    }

    private IMongoDatabase GetDatabase(string? databaseName = null)
    {
        if (!string.IsNullOrEmpty(databaseName))
        {
            return _client.GetDatabase(databaseName);
        }

        // Extract database name from connection string
        var url = MongoUrl.Create(_connectionString);
        if (string.IsNullOrEmpty(url.DatabaseName))
        {
            throw new InvalidOperationException("Database name must be specified either in connection string or in parameters.");
        }

        return _client.GetDatabase(url.DatabaseName);
    }

    #region BaseSource Implementation

    protected override async Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams @params)
    {
        var mongoParams = @params as MongoDBParams;
        if (mongoParams == null)
        {
            throw new ArgumentException("Invalid parameters type. Expected MongoDBParams.");
        }

        async Task<BaseDataSourceParams> ExecuteCoreAsync(CancellationToken ct)
        {
            var database = GetDatabase(mongoParams.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(mongoParams.CollectionName);

            long affectedCount = 0;

            switch (mongoParams.OperationType)
            {
                case MongoOperationType.InsertOne:
                    if (mongoParams.Document == null)
                    {
                        throw new InvalidOperationException("Document must be provided for InsertOne operation.");
                    }
                    await collection.InsertOneAsync(mongoParams.Document, cancellationToken: ct).ConfigureAwait(false);
                    affectedCount = 1;
                    break;

                case MongoOperationType.InsertMany:
                    if (mongoParams.Documents == null || mongoParams.Documents.Count == 0)
                    {
                        throw new InvalidOperationException("Documents must be provided for InsertMany operation.");
                    }
                    await collection.InsertManyAsync(mongoParams.Documents, cancellationToken: ct).ConfigureAwait(false);
                    affectedCount = mongoParams.Documents.Count;
                    break;

                case MongoOperationType.UpdateOne:
                    if (mongoParams.Filter == null || mongoParams.Update == null)
                    {
                        throw new InvalidOperationException("Filter and Update must be provided for UpdateOne operation.");
                    }
                    var updateOneResult = await collection.UpdateOneAsync(mongoParams.Filter, mongoParams.Update, cancellationToken: ct).ConfigureAwait(false);
                    affectedCount = updateOneResult.ModifiedCount;
                    break;

                case MongoOperationType.UpdateMany:
                    if (mongoParams.Filter == null || mongoParams.Update == null)
                    {
                        throw new InvalidOperationException("Filter and Update must be provided for UpdateMany operation.");
                    }
                    var updateManyResult = await collection.UpdateManyAsync(mongoParams.Filter, mongoParams.Update, cancellationToken: ct).ConfigureAwait(false);
                    affectedCount = updateManyResult.ModifiedCount;
                    break;

                case MongoOperationType.DeleteOne:
                    if (mongoParams.Filter == null)
                    {
                        throw new InvalidOperationException("Filter must be provided for DeleteOne operation.");
                    }
                    var deleteOneResult = await collection.DeleteOneAsync(mongoParams.Filter, cancellationToken: ct).ConfigureAwait(false);
                    affectedCount = deleteOneResult.DeletedCount;
                    break;

                case MongoOperationType.DeleteMany:
                    if (mongoParams.Filter == null)
                    {
                        throw new InvalidOperationException("Filter must be provided for DeleteMany operation.");
                    }
                    var deleteManyResult = await collection.DeleteManyAsync(mongoParams.Filter, cancellationToken: ct).ConfigureAwait(false);
                    affectedCount = deleteManyResult.DeletedCount;
                    break;

                default:
                    throw new InvalidOperationException($"Operation type {mongoParams.OperationType} is not supported for ExecuteNonQuery.");
            }

            mongoParams.SetValue(affectedCount);
            return mongoParams;
        }

        if (_resiliencePolicy == null)
        {
            return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
    }

    protected override async Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {
        var mongoParams = @params as MongoDBParams;
        if (mongoParams == null)
        {
            throw new ArgumentException("Invalid parameters type. Expected MongoDBParams.");
        }

        async Task<BaseDataSourceParams> ExecuteCoreAsync(CancellationToken ct)
        {
            var database = GetDatabase(mongoParams.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(mongoParams.CollectionName);

            switch (mongoParams.OperationType)
            {
                case MongoOperationType.Find:
                    var filter = mongoParams.Filter ?? Builders<BsonDocument>.Filter.Empty;
                    var findOptions = new FindOptions<BsonDocument, BsonDocument>
                    {
                        Projection = mongoParams.Projection,
                        Sort = mongoParams.Sort,
                        Skip = mongoParams.Skip,
                        Limit = mongoParams.Limit
                    };

                    var cursor = await collection.FindAsync(filter, findOptions, ct).ConfigureAwait(false);
                    var documents = await cursor.ToListAsync(ct).ConfigureAwait(false);

                    if (documents.Count == 1)
                    {
                        mongoParams.SetValue(BsonDocumentToDictionary(documents[0]));
                    }
                    else if (documents.Count > 1)
                    {
                        mongoParams.SetValue(documents.Select(BsonDocumentToDictionary).ToList());
                    }
                    else
                    {
                        mongoParams.SetValue(new Dictionary<string, object>());
                    }
                    break;

                case MongoOperationType.Aggregate:
                    if (mongoParams.Pipeline == null)
                    {
                        throw new InvalidOperationException("Pipeline must be provided for Aggregate operation.");
                    }
                    var aggregateCursor = await collection.AggregateAsync(mongoParams.Pipeline, cancellationToken: ct).ConfigureAwait(false);
                    var aggregateResults = await aggregateCursor.ToListAsync(ct).ConfigureAwait(false);

                    if (aggregateResults.Count == 1)
                    {
                        mongoParams.SetValue(BsonDocumentToDictionary(aggregateResults[0]));
                    }
                    else if (aggregateResults.Count > 1)
                    {
                        mongoParams.SetValue(aggregateResults.Select(BsonDocumentToDictionary).ToList());
                    }
                    else
                    {
                        mongoParams.SetValue(new Dictionary<string, object>());
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Operation type {mongoParams.OperationType} is not supported for ExecuteReader.");
            }

            return mongoParams;
        }

        if (_resiliencePolicy == null)
        {
            return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
    }

    protected override async Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params)
    {
        var mongoParams = @params as MongoDBParams;
        if (mongoParams == null)
        {
            throw new ArgumentException("Invalid parameters type. Expected MongoDBParams.");
        }

        async Task<BaseDataSourceParams<TValue>> ExecuteCoreAsync(CancellationToken ct)
        {
            var database = GetDatabase(mongoParams.DatabaseName);
            var collection = database.GetCollection<TValue>(mongoParams.CollectionName);

            List<TValue> results = new List<TValue>();

            switch (mongoParams.OperationType)
            {
                case MongoOperationType.Find:
                    var filter = mongoParams.Filter != null
                        ? BsonDocumentFilterToTypedFilter<TValue>(mongoParams.Filter)
                        : Builders<TValue>.Filter.Empty;

                    var findOptions = new FindOptions<TValue, TValue>
                    {
                        Projection = mongoParams.Projection != null ? BsonDocumentProjectionToTypedProjection<TValue>(mongoParams.Projection) : null,
                        Sort = mongoParams.Sort != null ? BsonDocumentSortToTypedSort<TValue>(mongoParams.Sort) : null,
                        Skip = mongoParams.Skip,
                        Limit = mongoParams.Limit
                    };

                    var cursor = await collection.FindAsync(filter, findOptions, ct).ConfigureAwait(false);
                    results = await cursor.ToListAsync(ct).ConfigureAwait(false);
                    break;

                case MongoOperationType.Aggregate:
                    if (mongoParams.Pipeline == null)
                    {
                        throw new InvalidOperationException("Pipeline must be provided for Aggregate operation.");
                    }
                    var pipeline = BsonDocumentPipelineToTypedPipeline<TValue>(mongoParams.Pipeline);
                    var aggregateCursor = await collection.AggregateAsync(pipeline, cancellationToken: ct).ConfigureAwait(false);
                    results = await aggregateCursor.ToListAsync(ct).ConfigureAwait(false);
                    break;

                default:
                    throw new InvalidOperationException($"Operation type {mongoParams.OperationType} is not supported for ExecuteReader<TValue>.");
            }

            if (results.Count == 1)
            {
                mongoParams.SetValue(results[0]);
            }
            else if (results.Count > 1)
            {
                mongoParams.SetValue(results);
            }

            return (BaseDataSourceParams<TValue>)(object)mongoParams;
        }

        if (_resiliencePolicy == null)
        {
            return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
    }

    protected override async Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams @params)
    {
        var mongoParams = @params as MongoDBParams;
        if (mongoParams == null)
        {
            throw new ArgumentException("Invalid parameters type. Expected MongoDBParams.");
        }

        async Task<BaseDataSourceParams> ExecuteCoreAsync(CancellationToken ct)
        {
            var database = GetDatabase(mongoParams.DatabaseName);
            var collection = database.GetCollection<BsonDocument>(mongoParams.CollectionName);

            object? result = null;

            switch (mongoParams.OperationType)
            {
                case MongoOperationType.Count:
                    var filter = mongoParams.Filter ?? Builders<BsonDocument>.Filter.Empty;
                    result = await collection.CountDocumentsAsync(filter, cancellationToken: ct).ConfigureAwait(false);
                    break;

                case MongoOperationType.Aggregate:
                    if (mongoParams.Pipeline == null)
                    {
                        throw new InvalidOperationException("Pipeline must be provided for Aggregate operation.");
                    }
                    var aggregateCursor = await collection.AggregateAsync(mongoParams.Pipeline, cancellationToken: ct).ConfigureAwait(false);
                    var firstResult = await aggregateCursor.FirstOrDefaultAsync(ct).ConfigureAwait(false);
                    
                    if (firstResult != null && firstResult.ElementCount > 0)
                    {
                        // Return the first value from the document
                        result = firstResult.GetElement(0).Value.ToString();
                    }
                    break;

                default:
                    throw new InvalidOperationException($"Operation type {mongoParams.OperationType} is not supported for ExecuteScalar.");
            }

            if (result != null)
            {
                mongoParams.SetValue(result);
            }
            
            return mongoParams;
        }

        if (_resiliencePolicy == null)
        {
            return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
    }

    #endregion

    #region IDataSource Implementation

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            var database = GetDatabase();
            var command = new BsonDocument("ping", 1);
            await database.RunCommandAsync<BsonDocument>(command).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        if (@params is MongoDBParams mongoParams)
        {
            return CheckMongoHealthAsync(mongoParams);
        }

        return Task.FromResult(false);
    }

    private async Task<bool> CheckMongoHealthAsync(MongoDBParams mongoParams)
    {
        try
        {
            var database = GetDatabase(mongoParams.DatabaseName);
            var command = new BsonDocument("ping", 1);
            await database.RunCommandAsync<BsonDocument>(command).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteNonQuery(@params).ConfigureAwait(false);
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteReader(@params).ConfigureAwait(false);
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TValue : class, new()
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
    {
        var mongoParams = ConvertToMongoDBParams(@params);
        var result = await ExecuteReader<TValue>(mongoParams).ConfigureAwait(false);
        
        // Copy the result back to the original params
        if (result.Value != null)
        {
            foreach (var item in result.Value)
            {
                @params.SetValue(item);
                break; // Set first item for single result
            }
        }
        
        return @params;
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(BaseDataSourceParams<TValue> @params)
        where TValue : class, new()
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
    {
        return (TBaseDataSourceParams)await ExecuteReaderAsync<TValue>(@params).ConfigureAwait(false);
    }

    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params)
        where TValue : class, new()
    {
        var mongoParams = ConvertToMongoDBParams(@params);
        return await ExecuteReader<TValue>(mongoParams).ConfigureAwait(false);
    }

    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteScalar(@params).ConfigureAwait(false);
    }

    #endregion

    #region IDataSource<MongoDBParams> Implementation

    public async Task<MongoDBParams> ExecuteNonQueryAsync(MongoDBParams @params)
    {
        return (MongoDBParams)await ExecuteNonQuery(@params).ConfigureAwait(false);
    }

    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(MongoDBParams @params)
        where TValue : class, new()
    {
        return await ExecuteReader<TValue>(@params).ConfigureAwait(false);
    }

    public async Task<MongoDBParams> ExecuteReaderAsync(MongoDBParams @params)
    {
        return (MongoDBParams)await ExecuteReader(@params).ConfigureAwait(false);
    }

    public async Task<MongoDBParams> ExecuteScalarAsync(MongoDBParams @params)
    {
        return (MongoDBParams)await ExecuteScalar(@params).ConfigureAwait(false);
    }

    #endregion

    #region Helper Methods

    private static MongoDBParams ConvertToMongoDBParams(object @params)
    {
        // Try to cast to MongoDBParams first
        if (@params is MongoDBParams mongoParams)
        {
            return mongoParams;
        }

        // Try to extract properties using reflection for type safety
        var paramsType = @params.GetType();
        var mongoDbParams = new MongoDBParams();

        var collectionNameProp = paramsType.GetProperty("CollectionName");
        if (collectionNameProp != null)
        {
            mongoDbParams.CollectionName = collectionNameProp.GetValue(@params) as string ?? string.Empty;
        }

        var databaseNameProp = paramsType.GetProperty("DatabaseName");
        if (databaseNameProp != null)
        {
            mongoDbParams.DatabaseName = databaseNameProp.GetValue(@params) as string;
        }

        var filterProp = paramsType.GetProperty("Filter");
        if (filterProp != null)
        {
            mongoDbParams.Filter = filterProp.GetValue(@params) as FilterDefinition<BsonDocument>;
        }

        var projectionProp = paramsType.GetProperty("Projection");
        if (projectionProp != null)
        {
            mongoDbParams.Projection = projectionProp.GetValue(@params) as ProjectionDefinition<BsonDocument>;
        }

        var sortProp = paramsType.GetProperty("Sort");
        if (sortProp != null)
        {
            mongoDbParams.Sort = sortProp.GetValue(@params) as SortDefinition<BsonDocument>;
        }

        var skipProp = paramsType.GetProperty("Skip");
        if (skipProp != null)
        {
            mongoDbParams.Skip = skipProp.GetValue(@params) as int?;
        }

        var limitProp = paramsType.GetProperty("Limit");
        if (limitProp != null)
        {
            mongoDbParams.Limit = limitProp.GetValue(@params) as int?;
        }

        var operationTypeProp = paramsType.GetProperty("OperationType");
        if (operationTypeProp != null)
        {
            var opType = operationTypeProp.GetValue(@params);
            mongoDbParams.OperationType = opType != null ? (MongoOperationType)opType : MongoOperationType.Find;
        }

        var pipelineProp = paramsType.GetProperty("Pipeline");
        if (pipelineProp != null)
        {
            mongoDbParams.Pipeline = pipelineProp.GetValue(@params) as PipelineDefinition<BsonDocument, BsonDocument>;
        }

        return mongoDbParams;
    }

    private static Dictionary<string, object> BsonDocumentToDictionary(BsonDocument document)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var element in document.Elements)
        {
            dictionary[element.Name] = BsonValueToObject(element.Value);
        }

        return dictionary;
    }

    private static object BsonValueToObject(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.ObjectId => value.AsObjectId.ToString(),
            BsonType.String => value.AsString,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Double => value.AsDouble,
            BsonType.Decimal128 => Decimal128.ToDecimal(value.AsDecimal128),
            BsonType.Boolean => value.AsBoolean,
            BsonType.DateTime => value.ToUniversalTime(),
            BsonType.Array => value.AsBsonArray.Select(BsonValueToObject).ToList(),
            BsonType.Document => BsonDocumentToDictionary(value.AsBsonDocument),
            BsonType.Null => null!,
            _ => value.ToString()!
        };
    }

    private static FilterDefinition<TValue> BsonDocumentFilterToTypedFilter<TValue>(FilterDefinition<BsonDocument> bsonFilter)
    {
        // Convert BsonDocument filter to typed filter by rendering and re-parsing
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
        var rendered = bsonFilter.Render(new(serializer, BsonSerializer.SerializerRegistry));
        return new BsonDocumentFilterDefinition<TValue>(rendered);
    }

    private static ProjectionDefinition<TValue>? BsonDocumentProjectionToTypedProjection<TValue>(ProjectionDefinition<BsonDocument> bsonProjection)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
        var rendered = bsonProjection.Render(new(serializer, BsonSerializer.SerializerRegistry));
        return new BsonDocumentProjectionDefinition<TValue>(rendered);
    }

    private static SortDefinition<TValue>? BsonDocumentSortToTypedSort<TValue>(SortDefinition<BsonDocument> bsonSort)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
        var rendered = bsonSort.Render(new(serializer, BsonSerializer.SerializerRegistry));
        return new BsonDocumentSortDefinition<TValue>(rendered);
    }

    private static PipelineDefinition<TValue, TValue> BsonDocumentPipelineToTypedPipeline<TValue>(PipelineDefinition<BsonDocument, BsonDocument> bsonPipeline)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<BsonDocument>();
        var rendered = bsonPipeline.Render(new(serializer, BsonSerializer.SerializerRegistry));
        var stages = rendered.Documents.Select(doc => new BsonDocumentPipelineStageDefinition<TValue, TValue>(doc)).ToList();
        return new PipelineStagePipelineDefinition<TValue, TValue>(stages);
    }

    #endregion
}