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

