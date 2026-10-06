using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessProvider.Core.DataSource;

public sealed record DataSourceRegistration(Type Parameters, Type Source);
public sealed class DataSourceRegistry
{
    private readonly ConcurrentDictionary<Type, Type> _types = new();
    public DataSourceRegistry(IEnumerable<DataSourceRegistration> registrations)
    {
        _types[typeof(JsonFileSourceParams)] = typeof(JsonFileSource);
        _types[typeof(JsonFileSourceParams<>)] = typeof(JsonFileSource);
        _types[typeof(StaticCodeParams)] = typeof(StaticCodeSource);
        _types[typeof(StaticCodeParams<>)] = typeof(StaticCodeSource);
        var explicitMappings = new Dictionary<Type, Type>();
        foreach (var registration in registrations)
        {
            if (explicitMappings.TryGetValue(registration.Parameters, out var existing) && existing != registration.Source)
                throw new InvalidOperationException($"Conflicting source mapping for {registration.Parameters.FullName}.");
            explicitMappings[registration.Parameters] = registration.Source;
            _types[registration.Parameters] = registration.Source;
        }
    }
    public IReadOnlyDictionary<Type, Type> Snapshot => new ReadOnlyDictionary<Type, Type>(new Dictionary<Type, Type>(_types));
    internal void Register(Type parameters, Type source) => _types[parameters.IsGenericType ? parameters.GetGenericTypeDefinition() : parameters] = source;
    internal bool TryGet(Type type, out Type source) => _types.TryGetValue(type, out source!) || (type.IsGenericType && _types.TryGetValue(type.GetGenericTypeDefinition(), out source!));
}
[Obsolete("Migrate to the 1.4 client, command and result API before 2.0; see docs/migration.md.", DiagnosticId = "DAP001")]
public class DataSourceFactory : IDataSourceFactory
{
    private readonly IServiceProvider _provider;
    private readonly DataSourceRegistry _registry;
    public DataSourceFactory(IServiceProvider serviceProvider) : this(serviceProvider, new DataSourceRegistry([])) { }
    public DataSourceFactory(IServiceProvider serviceProvider, DataSourceRegistry registry) { _provider = serviceProvider; _registry = registry; }
    public Dictionary<string, Type> GetRegisteredDataSources()
    {
        var result = new Dictionary<string, Type>();
        foreach (var group in _registry.Snapshot.GroupBy(x => x.Key.Name))
            foreach (var entry in group) result[group.Count() == 1 ? entry.Key.Name : entry.Key.FullName!] = entry.Value;
        return result;
    }
    public void RegisterDataSource<TParams, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>() where TParams : BaseDataSourceParams where TSource : IDataSource
        => _registry.Register(typeof(TParams), typeof(TSource));
    public IDataSource CreateDataSource(BaseDataSourceParams p) => Resolve<IDataSource>(p.GetType());
    public IDataSource CreateDataSource<TValue>(BaseDataSourceParams<TValue> p) where TValue : class => Resolve<IDataSource>(p.GetType());
    IDataSource<TParams> IDataSourceFactory.CreateDataSource<TParams>() => Resolve<IDataSource<TParams>>(typeof(TParams));
    public IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams
    {
        var ctor = typeof(IBaseDataSourceParams).GetConstructor(Type.EmptyTypes);
        if (typeof(IBaseDataSourceParams).IsAbstract || ctor is null) throw new InvalidOperationException($"{typeof(IBaseDataSourceParams).Name} requires a public parameterless constructor.");
        return (IBaseDataSourceParams)ctor.Invoke(null);
    }
    private T Resolve<T>(Type parameters)
    {
        if (!_registry.TryGet(parameters, out var source))
        {
            var expected = parameters.Name.Split('`')[0];
            if (expected.EndsWith("Params", StringComparison.Ordinal)) expected = expected[..^6];
            var matches = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a =>
            {
                try { return a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>().ToArray(); }
            }).Where(t => t.Name == expected && typeof(IDataSource).IsAssignableFrom(t) && !t.IsAbstract).ToArray();
            if (matches.Length != 1) throw new ArgumentException($"Unsupported data source type: {parameters.Name}; register an explicit type mapping.");
            source = matches[0]; _registry.Register(parameters, source);
        }
        // The injected provider belongs to the caller's scope; this factory never creates or disposes that scope.
        return (T)_provider.GetRequiredService(source);
    }
}
