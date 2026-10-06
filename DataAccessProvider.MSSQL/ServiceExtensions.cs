using DataAccessProvider.Core;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
namespace DataAccessProvider.MSSQL;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderMSSQL(this IServiceCollection services, IConfiguration configuration)
        => services.AddDataAccessProviderMSSQL(configuration.GetConnectionString(nameof(MSSQLSource)) ?? string.Empty);
    public static IServiceCollection AddDataAccessProviderMSSQL(this IServiceCollection services, string connectionString)
        => services.AddDataAccessProviderMSSQL(connectionString, new DatabaseClientOptions());
    public static IServiceCollection AddDataAccessProviderMSSQL(this IServiceCollection services, string connectionString, DatabaseClientOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        options = ProviderRegistration.Snapshot(options);
        services.AddDataAccessProviderCore();
        if (!ProviderRegistration.Ensure(services, typeof(SqlServer), connectionString, options)) return services;
        services.TryAddScoped(sp => new MSSQLSource(connectionString, sp.GetService<IResiliencePolicy>()));
        services.TryAddScoped<IDataSource<MSSQLSourceParams>>(sp => sp.GetRequiredService<MSSQLSource>());
        services.TryAddScoped<IDatabaseTransactionProvider<MSSQLSourceParams>>(sp => sp.GetRequiredService<MSSQLSource>());
        services.TryAddScoped(sp => new SqlServerClient(connectionString, options));
        services.TryAddScoped<IDatabaseClient<SqlServer>>(sp => sp.GetRequiredService<SqlServerClient>());
        services.AddSingleton(new DataSourceRegistration(typeof(MSSQLSourceParams), typeof(MSSQLSource)));
        services.AddSingleton(new DataSourceRegistration(typeof(MSSQLSourceParams<>), typeof(MSSQLSource)));
        return services;
    }
    public static IServiceProvider UseDataAccessProviderMSSQL(this IServiceProvider provider) => provider;

}
