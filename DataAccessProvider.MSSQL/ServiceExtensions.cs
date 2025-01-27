using Microsoft.Extensions.DependencyInjection;
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
        service.TryAddScoped<IDataSourceFactory, DataSourceFactory>();

        // Add database source service
        string mssqlString = configuration.GetConnectionString(nameof(MSSQLSource)) ?? "";

        service.AddScoped<IDataSource<MSSQLSourceParams>, MSSQLSource>(provider => new MSSQLSource(mssqlString));

        // Register necessary services
        service.AddScoped(factory => new MSSQLSource(mssqlString));

        // Register MSSQLSource
        service.AddScoped(provider =>
        {
            var factory = provider.GetRequiredService<IDataSourceFactory>();
            factory.RegisterDataSource<MSSQLSourceParams, MSSQLSource>();
            return factory;
        });

        return service;
    }
}

