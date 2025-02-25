﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.DataSource;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessProvider.MSSQL;
public static class ServiceExtensions
{

    public static IServiceCollection AddDataAccessProviderMSSQL(this IServiceCollection service, IConfiguration configuration)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        // Add database source service
        string mssqlString = configuration.GetConnectionString(nameof(MSSQLSource)) ?? "";

        // Register necessary services
        service.AddScoped<IDataSource<MSSQLSourceParams>, MSSQLSource>(provider => new MSSQLSource(mssqlString));
        service.AddScoped(factory => new MSSQLSource(mssqlString));

        return service;
    }

    public static IServiceCollection AddDataAccessProviderMSSQL(this IServiceCollection service, string connectionString)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        // Register necessary services
        service.AddScoped<IDataSource<MSSQLSourceParams>, MSSQLSource>(provider => new MSSQLSource(connectionString));
        service.AddScoped(factory => new MSSQLSource(connectionString));

        return service;
    }

    /// <summary>
    /// Registers the MSSQL data source with the IDataSourceFactory.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The service provider.</returns>
    /// <exception cref="InvalidOperationException">Thrown if IDataSourceFactory is not registered.</exception>
    public static IServiceProvider UseDataAccessProviderMSSQL(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDataSourceFactory>();
        if (factory == null)
        {
            throw new InvalidOperationException("IDataSourceFactory is not registered. Use AddDataAccessProviderMSSQL");
        }
        factory.RegisterDataSource<MSSQLSourceParams, MSSQLSource>();
        return provider;
    }
}

