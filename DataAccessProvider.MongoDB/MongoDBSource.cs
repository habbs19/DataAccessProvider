
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;

namespace DataAccessProvider.MongoDB;

#region Props
public partial class MongoDBSource : BaseSource
{
    protected override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams @params)
    {
        throw new NotImplementedException();
    }

    protected override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {
        throw new NotImplementedException();
    }

    protected override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params)
    {
        throw new NotImplementedException();
    }

    protected override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams @params)
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

    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(BaseDataSourceParams<TValue> @params)
        where TValue : class, new()
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
    {
        throw new NotImplementedException();
    }

    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
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
    public async Task<MongoDBParams> ExecuteNonQueryAsync(MongoDBParams @params)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(MongoDBParams @params) where TValue : class, new()
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public async Task<MongoDBParams> ExecuteReaderAsync(MongoDBParams @params)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }

    public async Task<MongoDBParams> ExecuteScalarAsync(MongoDBParams @params)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
#endregion MongoDBSource<>