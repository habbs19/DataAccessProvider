using DataAccessProvider.Core;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace DataAccessProvider.Postgres;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderPostgres(this IServiceCollection services, IConfiguration configuration)
        => services.AddDataAccessProviderPostgres(configuration.GetConnectionString(nameof(PostgresSource)) ?? string.Empty);
    public static IServiceCollection AddDataAccessProviderPostgres(this IServiceCollection services, string connectionString)
        => services.AddDataAccessProviderPostgres(connectionString, new DatabaseClientOptions());
    public static IServiceCollection AddDataAccessProviderPostgres(this IServiceCollection services, string connectionString, DatabaseClientOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options = ProviderRegistration.Snapshot(options);
        services.AddDataAccessProviderCore();
        if (!ProviderRegistration.Ensure(services, typeof(PostgreSql), connectionString, options)) return services;
        services.TryAddScoped(sp => new PostgresSource(connectionString, sp.GetService<IResiliencePolicy>()));
        services.TryAddScoped<IDataSource<PostgresSourceParams>>(sp => sp.GetRequiredService<PostgresSource>());
        services.TryAddScoped<IDatabaseTransactionProvider<PostgresSourceParams>>(sp => sp.GetRequiredService<PostgresSource>());
        services.TryAddScoped(sp => new PostgreSqlClient(connectionString, options));
        services.TryAddScoped<IDatabaseClient<PostgreSql>>(sp => sp.GetRequiredService<PostgreSqlClient>());
        services.AddSingleton(new DataSourceRegistration(typeof(PostgresSourceParams), typeof(PostgresSource)));
        services.AddSingleton(new DataSourceRegistration(typeof(PostgresSourceParams<>), typeof(PostgresSource)));
        return services;
    }
    public static IServiceProvider UseDataAccessProviderPostgres(this IServiceProvider provider) => provider;

}
