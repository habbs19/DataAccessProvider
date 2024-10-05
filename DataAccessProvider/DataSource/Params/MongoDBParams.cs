using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public class MongoDBParams : BaseDataSourceParams
{
}

public class MongoDBParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class
{
}
