using DataAccessProvider.Interfaces;
using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;

namespace DataAccessProvider.DataSource;

public class DataSourceFactory : IDataSourceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DataSourceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IDatabase<TDataSourceType, TDbParameter> CreateDataSource<TDataSourceType, TDbParameter>(DataSourceTypeEnum sourceType)
        where TDataSourceType : DataSourceType
        where TDbParameter : DbParameter
    {
        return sourceType switch
        {
            DataSourceTypeEnum.MSSQL => (IDatabase<TDataSourceType, TDbParameter>)_serviceProvider.GetService<IDatabaseMSSQL>()!,
            DataSourceTypeEnum.Postgres => (IDatabase<TDataSourceType, TDbParameter>)_serviceProvider.GetService<IDatabasePostgres>()!,
            DataSourceTypeEnum.JsonFile => (IDatabase<TDataSourceType, TDbParameter>)_serviceProvider.GetService<IJsonFileSource>()!,
            _ => throw new ArgumentException($"Unsupported data source type: {sourceType}")
        };
    }
}
