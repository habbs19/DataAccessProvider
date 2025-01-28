using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.DataSource;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace DataAccessProvider.MySql;
public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderMySql(this IServiceCollection service, IConfiguration configuration)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        // Add database source service
        string mssqlString = configuration.GetConnectionString(nameof(MySQLSource)) ?? "";

        // Register necessary services
        service.AddScoped<IDataSource<MySQLSourceParams>, MySQLSource>(provider => new MySQLSource(mssqlString));
        service.AddScoped(factory => new MySQLSource(mssqlString));

        return service;
    }

    public static IServiceCollection AddDataAccessProviderMySql(this IServiceCollection service, string connectionString)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        // Register necessary services
        service.AddScoped<IDataSource<MySQLSourceParams>, MySQLSource>(provider => new MySQLSource(connectionString));
        service.AddScoped(factory => new MySQLSource(connectionString));

        return service;
    }

    public static IServiceProvider UseDataAccessProviderMySql(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDataSourceFactory>();
        if (factory == null)
        {
            throw new InvalidOperationException("IDataSourceFactory is not registered. Use AddDataAccessProviderMySql");
        }
        factory.RegisterDataSource<MySQLSourceParams, MySQLSource>();
        return provider;
    }
}

