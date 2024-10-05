using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.DataSource.Source;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
using DataAccessProvider.Types;
using Google.Protobuf.Compiler;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Reflection;

namespace DataAccessProvider.DataSource;

public class DataSourceFactory : IDataSourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, Type> _dataSourceMappings = new();

    public DataSourceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Default data source mappings
        _dataSourceMappings.Add(typeof(MSSQLSourceParams), typeof(MSSQLSource));
        _dataSourceMappings.Add(typeof(PostgresSourceParams), typeof(PostgresSource));
        _dataSourceMappings.Add(typeof(JsonFileSourceParams), typeof(JsonFileSource));
        _dataSourceMappings.Add(typeof(MySQLSourceParams), typeof(MySQLSource));
        _dataSourceMappings.Add(typeof(StaticCodeParams), typeof(StaticCodeSource));
        _dataSourceMappings.Add(typeof(OracleSourceParams), typeof(OracleDataSource));
    }

    public void RegisterDataSource<TParams, TSource>()
     where TParams : BaseDataSourceParams
     where TSource : IDataSource
    {
        _dataSourceMappings[typeof(TParams)] = typeof(TSource);
    }

    public IDataSource CreateDataSource(BaseDataSourceParams baseDataSourceParams)
    {
        var paramType = baseDataSourceParams.GetType();

        // Check if there is a registered mapping for the given parameter type
        if (_dataSourceMappings.TryGetValue(paramType, out var dataSourceType))
        {
            var dataSource = _serviceProvider.GetService(dataSourceType);
            if (dataSource == null)
            {
                throw new InvalidOperationException($"{dataSourceType.Name} not found in service provider");
            }
            return (IDataSource)dataSource;
        }

        throw new ArgumentException($"Unsupported data source type: {paramType.Name}");
    }

    public IDataSource<T> CreateDataSource<T>() where T : BaseDataSourceParams
    {
        var paramType = typeof(T);

        // Check if there is a registered mapping for the given parameter type
        if (_dataSourceMappings.TryGetValue(paramType, out var dataSourceType))
        {
            var dataSource = _serviceProvider.GetService(dataSourceType);
            if (dataSource == null)
            {
                throw new InvalidOperationException($"{dataSourceType.Name} not found in service provider");
            }
            return (IDataSource<T>)dataSource;
        }

        throw new ArgumentException($"Unsupported data source type: {paramType.Name}");
    }

    public IDataSource CreateDataSource<TValue>(BaseDataSourceParams<TValue> baseDataSourceParams) where TValue : class
    {
        // Get the actual runtime type
        var type = baseDataSourceParams.GetType();

        // Get the clean type name: if it's generic, remove the backtick notation (`1)
        var typeName = type.IsGenericType
                ? type.GetGenericTypeDefinition().Name.Split('`')[0]  // Removes the "`1" part
                : type.Name;

        return typeName switch
        {
            nameof(MSSQLSourceParams) => (IDataSource)_serviceProvider.GetService<MSSQLSource>()!,
            nameof(PostgresSourceParams) => (IDataSource)_serviceProvider.GetService<PostgresSource>()!,
            nameof(JsonFileSourceParams) => (IDataSource)_serviceProvider.GetService<JsonFileSource>()!,
            nameof(MySQLSourceParams) => (IDataSource)_serviceProvider.GetService<MySQLSource>()!,
            nameof(StaticCodeParams) => (IDataSource)_serviceProvider.GetService<StaticCodeSource>()!,
            nameof(OracleSourceParams) => (IDataSource)_serviceProvider.GetService<OracleDataSource>()!,
            _ => throw new ArgumentException($"Unsupported data source type: {typeName}")
        };
    }

    public IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}

