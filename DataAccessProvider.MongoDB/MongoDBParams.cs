using DataAccessProvider.Core.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DataAccessProvider.MongoDB;

public class MongoDBParams : BaseDataSourceParams
{
    /// <summary>
    /// The name of the collection to operate on
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// The database name to use. If not specified, uses the database from connection string
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Filter definition for queries (find, update, delete operations)
    /// </summary>
    public FilterDefinition<BsonDocument>? Filter { get; set; }

    /// <summary>
    /// Document to insert or update
    /// </summary>
    public BsonDocument? Document { get; set; }

    /// <summary>
    /// Multiple documents to insert
    /// </summary>
    public List<BsonDocument>? Documents { get; set; }

    /// <summary>
    /// Update definition for update operations
    /// </summary>
    public UpdateDefinition<BsonDocument>? Update { get; set; }

    /// <summary>
    /// Projection definition for controlling which fields to return
    /// </summary>
    public ProjectionDefinition<BsonDocument>? Projection { get; set; }

    /// <summary>
    /// Sort definition for ordering results
    /// </summary>
    public SortDefinition<BsonDocument>? Sort { get; set; }

    /// <summary>
    /// Number of documents to skip
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Maximum number of documents to return
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Operation type (Find, InsertOne, InsertMany, UpdateOne, UpdateMany, DeleteOne, DeleteMany, Count, Aggregate)
    /// </summary>
    public MongoOperationType OperationType { get; set; } = MongoOperationType.Find;

    /// <summary>
    /// Pipeline for aggregation operations
    /// </summary>
    public PipelineDefinition<BsonDocument, BsonDocument>? Pipeline { get; set; }
}

public class MongoDBParams<TValue> : BaseDataSourceParams<TValue> where TValue : class
{
    /// <summary>
    /// The name of the collection to operate on
    /// </summary>
    public string CollectionName { get; set; } = string.Empty;

    /// <summary>
    /// The database name to use. If not specified, uses the database from connection string
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Filter definition for queries (find, update, delete operations)
    /// </summary>
    public FilterDefinition<TValue>? Filter { get; set; }

    /// <summary>
    /// Document to insert or update
    /// </summary>
    public TValue? Document { get; set; }

    /// <summary>
    /// Multiple documents to insert
    /// </summary>
    public List<TValue>? Documents { get; set; }

    /// <summary>
    /// Update definition for update operations
    /// </summary>
    public UpdateDefinition<TValue>? Update { get; set; }

    /// <summary>
    /// Projection definition for controlling which fields to return
    /// </summary>
    public ProjectionDefinition<TValue>? Projection { get; set; }

    /// <summary>
    /// Sort definition for ordering results
    /// </summary>
    public SortDefinition<TValue>? Sort { get; set; }

    /// <summary>
    /// Number of documents to skip
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Maximum number of documents to return
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Operation type (Find, InsertOne, InsertMany, UpdateOne, UpdateMany, DeleteOne, DeleteMany, Count, Aggregate)
    /// </summary>
    public MongoOperationType OperationType { get; set; } = MongoOperationType.Find;

    /// <summary>
    /// Pipeline for aggregation operations
    /// </summary>
    public PipelineDefinition<TValue, TValue>? Pipeline { get; set; }
}

/// <summary>
/// MongoDB operation types
/// </summary>
public enum MongoOperationType
{
    Find,
    InsertOne,
    InsertMany,
    UpdateOne,
    UpdateMany,
    DeleteOne,
    DeleteMany,
    Count,
    Aggregate
}
