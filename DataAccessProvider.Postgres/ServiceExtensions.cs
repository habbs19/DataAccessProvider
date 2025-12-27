using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessProvider.Postgres;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderPostgres(this IServiceCollection service, IConfiguration configuration)
    {
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        string connectionString = configuration.GetConnectionString(nameof(PostgresSource)) ?? string.Empty;

        service.AddScoped<IDataSource<PostgresSourceParams>>(sp =>
        {
            var policy = sp.GetService<IResiliencePolicy>();
            return new PostgresSource(connectionString, policy);
        });
        service.AddScoped(sp =>
        {
            var policy = sp.GetService<IResiliencePolicy>();
            return new PostgresSource(connectionString, policy);
        });

        return service;
    }

    public static IServiceCollection AddDataAccessProviderPostgres(this IServiceCollection service, string connectionString)
    {
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        service.AddScoped<IDataSource<PostgresSourceParams>>(sp =>
        {
            var policy = sp.GetService<IResiliencePolicy>();
            return new PostgresSource(connectionString, policy);
        });
        service.AddScoped(sp =>
        {
            var policy = sp.GetService<IResiliencePolicy>();
            return new PostgresSource(connectionString, policy);
        });

        return service;
    }

    /// <summary>
    /// Registers the Postgres data source with the IDataSourceFactory.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The service provider.</returns>
    public static IServiceProvider UseDataAccessProviderPostgres(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDataSourceFactory>();
        factory.RegisterDataSource<PostgresSourceParams, PostgresSource>();
        return provider;
    }
}
