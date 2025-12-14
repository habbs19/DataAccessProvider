using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessProvider.Core.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderCore(this IServiceCollection service)
    {
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        // Resilience policy (default)
        service.TryAddSingleton<IResiliencePolicy>(_ =>
            new BasicResiliencePolicy(maxRetries: 3, perAttemptTimeout: TimeSpan.FromSeconds(30)));

        service.AddScoped<JsonFileSource>();
        service.AddScoped<StaticCodeSource>();

        return service;
    }
}

