using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public class MongoDBParams : MongoDBParams<object>
{
}

public class MongoDBParams<TValue> : BaseDataSourceParams<TValue> where TValue : class
{
}
