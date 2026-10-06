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

