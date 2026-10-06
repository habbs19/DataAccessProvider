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

