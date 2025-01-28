using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessProvider.Core.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderCore(this IServiceCollection service)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        service.AddScoped<JsonFileSource>();
        service.AddScoped<StaticCodeSource>();

        return service;
    }
}

