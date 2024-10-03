using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
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

 

    public IDataSource CreateDataSource()
    {
        return _serviceProvider.GetService<IDataSource>()!;
    }

    public IDataSource<IBaseDataSourceParams> CreateDataSource<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams
    {
        return typeof(IBaseDataSourceParams).Name switch
        {
            nameof(MSSQL) => (IDataSource<IBaseDataSourceParams>)_serviceProvider.GetService<IMSSQLSource<MSSQLSourceParams>>()!,
            nameof(Postgres) => (IDataSource<IBaseDataSourceParams>)_serviceProvider.GetService<IPostgresSource<PostgresSourceParams>>()!,
            nameof(JsonFile) => (IDataSource<IBaseDataSourceParams>)_serviceProvider.GetService<IDataSource<JsonFileSourceParams>>()!,
            _ => throw new ArgumentException($"Unsupported data source type: {nameof(IBaseDataSourceParams)}")
        };
    }

    public IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}

