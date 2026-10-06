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

