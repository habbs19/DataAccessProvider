using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public class MongoDBParams : BaseDataSourceParams
{
}

public class MongoDBParams<TValue> : BaseDataSourceParams<TValue> where TValue : class
{
}
