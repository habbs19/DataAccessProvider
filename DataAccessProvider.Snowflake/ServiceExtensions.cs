using DataAccessProvider.Core;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace DataAccessProvider.Snowflake;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderSnowflake(this IServiceCollection services, IConfiguration configuration)
        => services.AddDataAccessProviderSnowflake(configuration.GetConnectionString(nameof(SnowflakeSource)) ?? string.Empty);
    public static IServiceCollection AddDataAccessProviderSnowflake(this IServiceCollection services, string connectionString)
        => services.AddDataAccessProviderSnowflake(connectionString, new DatabaseClientOptions());
    public static IServiceCollection AddDataAccessProviderSnowflake(this IServiceCollection services, string connectionString, DatabaseClientOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options = ProviderRegistration.Snapshot(options);
        services.AddDataAccessProviderCore();
        if (!ProviderRegistration.Ensure(services, typeof(SnowflakeDatabase), connectionString, options)) return services;
        services.TryAddScoped(sp => new SnowflakeSource(connectionString, sp.GetService<IResiliencePolicy>()));
        services.TryAddScoped<IDataSource<SnowflakeSourceParams>>(sp => sp.GetRequiredService<SnowflakeSource>());
        services.TryAddScoped<IDatabaseTransactionProvider<SnowflakeSourceParams>>(sp => sp.GetRequiredService<SnowflakeSource>());
        services.TryAddScoped(sp => new SnowflakeClient(connectionString, options));
        services.TryAddScoped<IDatabaseClient<SnowflakeDatabase>>(sp => sp.GetRequiredService<SnowflakeClient>());
        services.AddSingleton(new DataSourceRegistration(typeof(SnowflakeSourceParams), typeof(SnowflakeSource)));
        services.AddSingleton(new DataSourceRegistration(typeof(SnowflakeSourceParams<>), typeof(SnowflakeSource)));
        return services;
    }
    public static IServiceProvider UseDataAccessProviderSnowflake(this IServiceProvider provider) => provider;

}
