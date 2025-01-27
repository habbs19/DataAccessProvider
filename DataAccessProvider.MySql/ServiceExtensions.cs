using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.DataSource;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessProvider.MySql;
public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderMySql(this IServiceCollection service, IConfiguration configuration)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddScoped<IDataSourceFactory, DataSourceFactory>();

        // Add database source service
        string mssqlString = configuration.GetConnectionString(nameof(MySQLSource)) ?? "";

        service.AddScoped<IDataSource<MySQLSourceParams>, MySQLSource>(provider => new MySQLSource(mssqlString));

        // Register necessary services
        service.AddScoped(factory => new MySQLSource(mssqlString));

        // Register MSSQLSource
        service.AddScoped(provider =>
        {
            var factory = provider.GetRequiredService<IDataSourceFactory>();
            factory.RegisterDataSource<MySQLSourceParams, MySQLSource>();
            return factory;
        });

        return service;
    }
}

