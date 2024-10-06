using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.DataSource.Source;
using DataAccessProvider.Extensions;
using DataAccessProvider.Interfaces;

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
        _dataSourceMappings.Add(typeof(OracleSourceParams), typeof(OracleSource));

        // Add generic types
        _dataSourceMappings.Add(typeof(MSSQLSourceParams<>), typeof(MSSQLSource));
        _dataSourceMappings.Add(typeof(PostgresSourceParams<>), typeof(PostgresSource));
        _dataSourceMappings.Add(typeof(JsonFileSourceParams<>), typeof(JsonFileSource));
        _dataSourceMappings.Add(typeof(MySQLSourceParams<>), typeof(MySQLSource));
        _dataSourceMappings.Add(typeof(StaticCodeParams<>), typeof(StaticCodeSource));
        _dataSourceMappings.Add(typeof(OracleSourceParams<>), typeof(OracleSource));
    }

    public void RegisterDataSource<TParams, TSource>()
     where TParams : BaseDataSourceParams
     where TSource : IDataSource
    {
        // Register the non-generic type
        _dataSourceMappings[typeof(TParams)] = typeof(TSource);

        // Check if TParams is a generic type definition and register the generic type
        if (typeof(TParams).IsGenericTypeDefinition)
        {
            _dataSourceMappings[typeof(TParams).GetGenericTypeDefinition()] = typeof(TSource);
        }
        else if (typeof(TParams).IsGenericType)
        {
            _dataSourceMappings[typeof(TParams).GetGenericTypeDefinition()] = typeof(TSource);
        }
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

    public IDataSource CreateDataSource<TValue>(BaseDataSourceParams<TValue> baseDataSourceParams) where TValue : class
    {
        // Get the actual runtime type
        var type = baseDataSourceParams.GetType();


        // Get the clean type name: if it's generic, remove the backtick notation (`1)
        var typeName = type.IsGenericType
                ? type.GetGenericTypeDefinition().Name.Split('`')[0]  // Removes the "`1" part
                : type.Name;
        
        typeName = type.GetGenericTypeName();
        // Check if there is a registered mapping for the given parameter type
        if (_dataSourceMappings.TryGetValue(type, out var dataSourceType))
        {
            var dataSource = _serviceProvider.GetService(dataSourceType);
            if (dataSource == null)
            {
                throw new InvalidOperationException($"{dataSourceType.Name} not found in service provider");
            }
            return (IDataSource)dataSource;
        }
        throw new ArgumentException($"Unsupported data source type: {type.Name}");

    }

    public IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }

    IDataSource<TBaseDataSourceParams> IDataSourceFactory.CreateDataSource<TBaseDataSourceParams>()
    {
        var paramType = typeof(TBaseDataSourceParams);

        // Check if there is a registered mapping for the given parameter type
        if (_dataSourceMappings.TryGetValue(paramType, out var dataSourceType))
        {
            var dataSource = _serviceProvider.GetService(dataSourceType);
            if (dataSource == null)
            {
                throw new InvalidOperationException($"{dataSourceType.Name} not found in service provider");
            }
            return (IDataSource<TBaseDataSourceParams>)dataSource;
        }

        throw new ArgumentException($"Unsupported data source type: {paramType.Name}");
    }
}

