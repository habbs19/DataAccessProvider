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

