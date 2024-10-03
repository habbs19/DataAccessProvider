using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessProvider.DataSource.Source;

public class OracleDataSource : IDataSource, IDataSource<OracleSourceParams>
{
    public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }

    public Task<OracleSourceParams> ExecuteNonQueryAsync(OracleSourceParams @params)
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

    public Task<OracleSourceParams> ExecuteReaderAsync<TValue>(OracleSourceParams @params) where TValue : class, new()
    {
        throw new NotImplementedException();
    }

    public Task<OracleSourceParams> ExecuteReaderAsync(OracleSourceParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }

    public Task<OracleSourceParams> ExecuteScalarAsync(OracleSourceParams @params)
    {
        throw new NotImplementedException();
    }
}
