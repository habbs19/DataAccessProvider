using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;

namespace DataAccessProvider.DataSource.Source;

#region Props
public partial class MongoDBSource : BaseSource
{
    protected override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {
        throw new NotImplementedException();
    }

    protected override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params)
    {
        throw new NotImplementedException();
    }
}
#endregion Props
#region MongoDBSource
public partial class MongoDBSource : IDataSource
{
    public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }

    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TValue : class, new()
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
    {
        throw new NotImplementedException();
    }

    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }

    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}
#endregion MongoDBSource
#region MongoDBSource<>
public partial class MongoDBSource : IDataSource<MongoDBParams>
{
    public Task<MongoDBParams> ExecuteNonQueryAsync(MongoDBParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(MongoDBParams @params) where TValue : class, new()
    {
        throw new NotImplementedException();
    }

    public Task<MongoDBParams> ExecuteReaderAsync(MongoDBParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<MongoDBParams> ExecuteScalarAsync(MongoDBParams @params)
    {
        throw new NotImplementedException();
    }
}
#endregion MongoDBSource<>