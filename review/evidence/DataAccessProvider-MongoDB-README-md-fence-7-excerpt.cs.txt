var countParams = new MongoDBParams
{
    CollectionName = "users",
    Filter = Builders<BsonDocument>.Filter.Gte("age", 30),
    OperationType = MongoOperationType.Count
};
var result = await dataSourceProvider.ExecuteScalarAsync(countParams);

