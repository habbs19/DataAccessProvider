using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;

namespace DataAccessProvider.DataSource.Source;

public class StaticCodeSource : IDataSource<StaticCodeParams>, IDataSource
{
    public Task<StaticCodeParams> ExecuteNonQueryAsync(StaticCodeParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }

    public Task<StaticCodeParams> ExecuteReaderAsync<TValue>(StaticCodeParams @params) where TValue : class, new()
    {
        throw new NotImplementedException();
    }

    public Task<StaticCodeParams> ExecuteReaderAsync(StaticCodeParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
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

    public Task<StaticCodeParams> ExecuteScalarAsync(StaticCodeParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}
