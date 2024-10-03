using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.Common;

namespace DataAccessProvider.DataSource;

public class DataSourceFactory : IDataSourceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DataSourceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

 

    public IDataSource CreateDataSource(DataSourceTypeEnum sourceType)
    {
        switch(sourceType)
        {
            case DataSourceTypeEnum.MSSQL: return _serviceProvider.GetService<IDatabaseMSSQL>()!;                
            case DataSourceTypeEnum.JsonFile: return _serviceProvider.GetService<IJsonFileSource>()!;
            case DataSourceTypeEnum.PostgreSQL: return _serviceProvider.GetService<IDatabasePostgres>()!;
            default:
                return null!;

        }
    }

    public IDataSource<IBaseDataSourceParams> CreateDataSource<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams
    {
        return typeof(IBaseDataSourceParams).Name switch
        {
            nameof(MSSQL) => (IDataSource<IBaseDataSourceParams>)_serviceProvider.GetService<IDatabaseMSSQL<MSSQLSourceParams>>()!,
            nameof(Postgres) => (IDataSource<IBaseDataSourceParams>)_serviceProvider.GetService<IDatabasePostgres<PostgresSourceParams>>()!,
            nameof(JsonFile) => (IDataSource<IBaseDataSourceParams>)_serviceProvider.GetService<IJsonFileSource<JsonFileSourceParams>>()!,
            _ => throw new ArgumentException($"Unsupported data source type: {nameof(IBaseDataSourceParams)}")
        };
    }

    public IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}

