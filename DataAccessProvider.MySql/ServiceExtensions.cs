using DataAccessProvider.Core;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace DataAccessProvider.MySql;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderMySql(this IServiceCollection services, IConfiguration configuration)
        => services.AddDataAccessProviderMySql(configuration.GetConnectionString(nameof(MySQLSource)) ?? string.Empty);
    public static IServiceCollection AddDataAccessProviderMySql(this IServiceCollection services, string connectionString)
        => services.AddDataAccessProviderMySql(connectionString, new DatabaseClientOptions());
    public static IServiceCollection AddDataAccessProviderMySql(this IServiceCollection services, string connectionString, DatabaseClientOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options = ProviderRegistration.Snapshot(options);
        services.AddDataAccessProviderCore();
        if (!ProviderRegistration.Ensure(services, typeof(MySql), connectionString, options)) return services;
        services.TryAddScoped(sp => new MySQLSource(connectionString, sp.GetService<IResiliencePolicy>()));
        services.TryAddScoped<IDataSource<MySQLSourceParams>>(sp => sp.GetRequiredService<MySQLSource>());
        services.TryAddScoped<IDatabaseTransactionProvider<MySQLSourceParams>>(sp => sp.GetRequiredService<MySQLSource>());
        services.TryAddScoped(sp => new MySqlClient(connectionString, options));
        services.TryAddScoped<IDatabaseClient<MySql>>(sp => sp.GetRequiredService<MySqlClient>());
        services.AddSingleton(new DataSourceRegistration(typeof(MySQLSourceParams), typeof(MySQLSource)));
        services.AddSingleton(new DataSourceRegistration(typeof(MySQLSourceParams<>), typeof(MySQLSource)));
        return services;
    }
    public static IServiceProvider UseDataAccessProviderMySql(this IServiceProvider provider) => provider;
    public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseDataAccessProviderMySql(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { ArgumentNullException.ThrowIfNull(app); return app; }
}
