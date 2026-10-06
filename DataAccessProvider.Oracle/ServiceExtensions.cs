using DataAccessProvider.Core;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace DataAccessProvider.Oracle;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderOracle(this IServiceCollection services, IConfiguration configuration)
        => services.AddDataAccessProviderOracle(configuration.GetConnectionString(nameof(OracleSource)) ?? string.Empty);
    public static IServiceCollection AddDataAccessProviderOracle(this IServiceCollection services, string connectionString)
        => services.AddDataAccessProviderOracle(connectionString, new DatabaseClientOptions());
    public static IServiceCollection AddDataAccessProviderOracle(this IServiceCollection services, string connectionString, DatabaseClientOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options = ProviderRegistration.Snapshot(options);
        services.AddDataAccessProviderCore();
        if (!ProviderRegistration.Ensure(services, typeof(OracleDatabase), connectionString, options)) return services;
        services.TryAddScoped(sp => new OracleSource(connectionString, sp.GetService<IResiliencePolicy>()));
        services.TryAddScoped<IDataSource<OracleSourceParams>>(sp => sp.GetRequiredService<OracleSource>());
        services.TryAddScoped<IDatabaseTransactionProvider<OracleSourceParams>>(sp => sp.GetRequiredService<OracleSource>());
        services.TryAddScoped(sp => new OracleClient(connectionString, options));
        services.TryAddScoped<IDatabaseClient<OracleDatabase>>(sp => sp.GetRequiredService<OracleClient>());
        services.AddSingleton(new DataSourceRegistration(typeof(OracleSourceParams), typeof(OracleSource)));
        services.AddSingleton(new DataSourceRegistration(typeof(OracleSourceParams<>), typeof(OracleSource)));
        return services;
    }
    public static IServiceProvider UseDataAccessProviderOracle(this IServiceProvider provider) => provider;

}
