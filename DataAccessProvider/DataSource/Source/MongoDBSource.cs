using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessProvider.DataSource.Source;

public class MongoDBSource : IDataSource<MongoDBParams>, IDataSource
{
    public MongoDBSource()
    {
    }

    public Task<MongoDBParams> ExecuteNonQueryAsync(MongoDBParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }

    public Task<MongoDBParams> ExecuteReaderAsync<TValue>(MongoDBParams @params) where TValue : class, new()
    {
        throw new NotImplementedException();
    }

    public Task<MongoDBParams> ExecuteReaderAsync(MongoDBParams @params)
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

    public Task<MongoDBParams> ExecuteScalarAsync(MongoDBParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}
