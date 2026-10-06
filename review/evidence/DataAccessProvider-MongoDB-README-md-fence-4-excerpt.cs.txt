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

