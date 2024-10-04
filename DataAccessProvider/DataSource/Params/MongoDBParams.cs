using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public class MongoDBParams : BaseDataSourceParams
{
}

public class MongoDBParams<TValue> : DatabaseSourceParams<TValue> where TValue : class
{
}
