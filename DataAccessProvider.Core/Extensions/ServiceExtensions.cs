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
    public static IServiceCollection AddDataAccessProviderCore(this IServiceCollection services)
    {
        services.TryAddSingleton<DataSourceRegistry>();
        services.TryAddScoped<IDataSourceFactory, DataSourceFactory>();
        services.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        services.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        services.TryAddScoped<JsonFileSource>(); services.TryAddScoped<StaticCodeSource>();
        services.TryAddSingleton<JsonFileClient>();
        return services;
    }
    public static IServiceCollection AddDataAccessProviderCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataAccessProviderCore();
        var options = new ResilienceOptions(); configuration.GetSection("DataAccessProvider:Resilience").Bind(options);
        services.TryAddSingleton<IResiliencePolicy>(_ => new BasicResiliencePolicy(options.MaxRetries, TimeSpan.FromSeconds(options.PerAttemptTimeoutSeconds)));
        return services;
    }
}
