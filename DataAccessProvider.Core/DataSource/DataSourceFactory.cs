using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessProvider.Core.DataSource;

public class DataSourceFactory : IDataSourceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, Type> _dataSourceMappings = new();

    public DataSourceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        // Default data source mappings
        _dataSourceMappings.Add(nameof(JsonFileSourceParams), typeof(JsonFileSource));
        _dataSourceMappings.Add(nameof(StaticCodeParams), typeof(StaticCodeSource));
    }

    public Dictionary<string, Type> GetRegisteredDataSources() => _dataSourceMappings;

    public void RegisterDataSource<TParams, TSource>()
     where TParams : BaseDataSourceParams
     where TSource : IDataSource
    {
        // Register the non-generic type
        _dataSourceMappings[typeof(TParams).Name] = typeof(TSource);

        // Check if TParams is a generic type definition and register the generic type
        if (typeof(TParams).IsGenericTypeDefinition)
        {
            var name = typeof(TParams).GetGenericTypeDefinition().GetCleanGenericTypeName();
            _dataSourceMappings[name] = typeof(TSource);
        }
        else if (typeof(TParams).IsGenericType)
        {
            var name = typeof(TParams).GetGenericTypeDefinition().GetCleanGenericTypeName();
            _dataSourceMappings[name] = typeof(TSource);
        }
    }

    public IDataSource CreateDataSource(BaseDataSourceParams baseDataSourceParams)
    {
        var paramType = baseDataSourceParams.GetType();

        // Check if there is a registered mapping for the given parameter type
        if (TryResolveDataSourceType(paramType, out var dataSourceType))
        {
            using var scope = _serviceProvider.CreateScope();
            var dataSource = scope.ServiceProvider.GetService(dataSourceType);
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

        // Check if there is a registered mapping for the given parameter type
        if (TryResolveDataSourceType(type, out var dataSourceType))
        {
            using var scope = _serviceProvider.CreateScope();
            var dataSource = scope.ServiceProvider.GetService(dataSourceType);
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
        if (TryResolveDataSourceType(paramType, out var dataSourceType))
        {
            using var scope = _serviceProvider.CreateScope();
            var dataSource = scope.ServiceProvider.GetService(dataSourceType);
            if (dataSource == null)
            {
                throw new InvalidOperationException($"{dataSourceType.Name} not found in service provider");
            }
            return (IDataSource<TBaseDataSourceParams>)dataSource;
        }

        throw new ArgumentException($"Unsupported data source type: {paramType.Name}");
    }

    private bool TryResolveDataSourceType(Type paramType, out Type dataSourceType)
    {
        var cleanName = paramType.GetCleanGenericTypeName();
        var name = paramType.Name;
        var genericName = paramType.GetGenericTypeName();

        if (_dataSourceMappings.TryGetValue(cleanName, out dataSourceType)
            || _dataSourceMappings.TryGetValue(name, out dataSourceType)
            || _dataSourceMappings.TryGetValue(genericName, out dataSourceType))
        {
            return true;
        }

        // Fallback: map by convention (e.g. MSSQLSourceParams -> MSSQLSource) when registered in DI.
        if (cleanName.EndsWith("Params", StringComparison.Ordinal))
        {
            var expectedSourceName = cleanName[..^"Params".Length];
            var candidate = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.Name == expectedSourceName
                    && typeof(IDataSource).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract);

            if (candidate is not null)
            {
                _dataSourceMappings[cleanName] = candidate;
                dataSourceType = candidate;
                return true;
            }
        }

        dataSourceType = default!;
        return false;
    }
}
