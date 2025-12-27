# DataAccessProvider.MongoDB

A flexible MongoDB data access provider that integrates seamlessly with the DataAccessProvider framework. This package provides a standardized interface for MongoDB operations including queries, inserts, updates, deletes, and aggregations.

## Features

- **Asynchronous Operations**: Full async/await support for all MongoDB operations
- **Type-Safe Queries**: Support for both strongly-typed and BsonDocument operations
- **Flexible Operations**: Support for Find, Insert, Update, Delete, Count, and Aggregate operations
- **LINQ Support**: Native MongoDB driver integration with full LINQ support
- **Resilience Support**: Built-in support for resilience policies (retry, circuit breaker, etc.)
- **Dependency Injection**: Easy integration with .NET dependency injection

## Installation

```bash
dotnet add package DataAccessProvider.MongoDB
```

## Configuration

### Add Connection String to appsettings.json

```json
{
  "ConnectionStrings": {
    "MongoDBSource": "mongodb://localhost:27017/myDatabase"
  }
}
```

### Register Services

```csharp
// In Startup.cs or Program.cs
services.AddDataAccessProviderMongoDB(configuration);

// Or with a direct connection string
services.AddDataAccessProviderMongoDB("mongodb://localhost:27017/myDatabase");

// Register the MongoDB data source with the factory
serviceProvider.UseDataAccessProviderMongoDB();
```

## Usage Examples

### Find Documents

```csharp
// Find all documents
var findParams = new MongoDBParams
{
    CollectionName = "users",
    OperationType = MongoOperationType.Find
};
var result = await dataSourceProvider.ExecuteReaderAsync(findParams);

// Find with filter
var findParamsWithFilter = new MongoDBParams
{
    CollectionName = "users",
    Filter = Builders<BsonDocument>.Filter.Eq("name", "John"),
    OperationType = MongoOperationType.Find
};
var filteredResult = await dataSourceProvider.ExecuteReaderAsync(findParamsWithFilter);

// Find with typed result
var typedParams = new MongoDBParams<User>
{
    CollectionName = "users",
    Filter = Builders<User>.Filter.Eq(u => u.Name, "John"),
    OperationType = MongoOperationType.Find
};
var typedResult = await dataSourceProvider.ExecuteReaderAsync(typedParams);
```

### Insert Documents

```csharp
// Insert one document
var insertParams = new MongoDBParams
{
    CollectionName = "users",
    Document = new BsonDocument { { "name", "John" }, { "age", 30 } },
    OperationType = MongoOperationType.InsertOne
};
await dataSourceProvider.ExecuteNonQueryAsync(insertParams);

// Insert many documents
var insertManyParams = new MongoDBParams
{
    CollectionName = "users",
    Documents = new List<BsonDocument>
    {
        new BsonDocument { { "name", "John" }, { "age", 30 } },
        new BsonDocument { { "name", "Jane" }, { "age", 25 } }
    },
    OperationType = MongoOperationType.InsertMany
};
await dataSourceProvider.ExecuteNonQueryAsync(insertManyParams);
```

### Update Documents

```csharp
// Update one document
var updateParams = new MongoDBParams
{
    CollectionName = "users",
    Filter = Builders<BsonDocument>.Filter.Eq("name", "John"),
    Update = Builders<BsonDocument>.Update.Set("age", 31),
    OperationType = MongoOperationType.UpdateOne
};
await dataSourceProvider.ExecuteNonQueryAsync(updateParams);

// Update many documents
var updateManyParams = new MongoDBParams
{
    CollectionName = "users",
    Filter = Builders<BsonDocument>.Filter.Gte("age", 30),
    Update = Builders<BsonDocument>.Update.Set("category", "senior"),
    OperationType = MongoOperationType.UpdateMany
};
await dataSourceProvider.ExecuteNonQueryAsync(updateManyParams);
```

### Delete Documents

```csharp
// Delete one document
var deleteParams = new MongoDBParams
{
    CollectionName = "users",
    Filter = Builders<BsonDocument>.Filter.Eq("name", "John"),
    OperationType = MongoOperationType.DeleteOne
};
await dataSourceProvider.ExecuteNonQueryAsync(deleteParams);

// Delete many documents
var deleteManyParams = new MongoDBParams
{
    CollectionName = "users",
    Filter = Builders<BsonDocument>.Filter.Lt("age", 18),
    OperationType = MongoOperationType.DeleteMany
};
await dataSourceProvider.ExecuteNonQueryAsync(deleteManyParams);
```

### Count Documents

```csharp
var countParams = new MongoDBParams
{
    CollectionName = "users",
    Filter = Builders<BsonDocument>.Filter.Gte("age", 30),
    OperationType = MongoOperationType.Count
};
var result = await dataSourceProvider.ExecuteScalarAsync(countParams);
```

### Aggregation

```csharp
var aggregateParams = new MongoDBParams
{
    CollectionName = "users",
    Pipeline = new[]
    {
        new BsonDocument("$match", new BsonDocument("age", new BsonDocument("$gte", 30))),
        new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$category" },
            { "count", new BsonDocument("$sum", 1) }
        })
    },
    OperationType = MongoOperationType.Aggregate
};
var result = await dataSourceProvider.ExecuteReaderAsync(aggregateParams);
```

### Advanced Options

```csharp
// With projection, sort, skip, and limit
var advancedParams = new MongoDBParams
{
    CollectionName = "users",
    Filter = Builders<BsonDocument>.Filter.Empty,
    Projection = Builders<BsonDocument>.Projection.Include("name").Include("email"),
    Sort = Builders<BsonDocument>.Sort.Descending("age"),
    Skip = 10,
    Limit = 20,
    OperationType = MongoOperationType.Find
};
var result = await dataSourceProvider.ExecuteReaderAsync(advancedParams);
```

## MongoDBParams Properties

- `CollectionName`: Name of the MongoDB collection
- `DatabaseName`: Optional database name (defaults to connection string database)
- `Filter`: Filter definition for queries
- `Document`: Single document for insert operations
- `Documents`: Multiple documents for bulk insert operations
- `Update`: Update definition for update operations
- `Projection`: Fields to include/exclude in results
- `Sort`: Sort order for results
- `Skip`: Number of documents to skip
- `Limit`: Maximum number of documents to return
- `OperationType`: Type of operation (Find, InsertOne, InsertMany, UpdateOne, UpdateMany, DeleteOne, DeleteMany, Count, Aggregate)
- `Pipeline`: Pipeline definition for aggregation operations

## License

This project is licensed under the MIT License.
