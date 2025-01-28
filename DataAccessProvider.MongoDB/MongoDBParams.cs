using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.MongoDB;

public class MongoDBParams : BaseDataSourceParams
{
}

public class MongoDBParams<TValue> : BaseDataSourceParams<TValue> where TValue : class
{
}
