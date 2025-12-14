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
    public static IServiceCollection AddDataAccessProviderCore(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        // Bind resilience options from configuration (section: "DataAccessProvider:Resilience")
        var resilienceOptions = new ResilienceOptions();
        configuration
            .GetSection("DataAccessProvider:Resilience")
            .Bind(resilienceOptions);

        service.TryAddSingleton<IResiliencePolicy>(_ =>
            new BasicResiliencePolicy(
                maxRetries: resilienceOptions.MaxRetries,
                perAttemptTimeout: TimeSpan.FromSeconds(resilienceOptions.PerAttemptTimeoutSeconds)));

        service.AddScoped<JsonFileSource>();
        service.AddScoped<StaticCodeSource>();

        return service;
    }
}

