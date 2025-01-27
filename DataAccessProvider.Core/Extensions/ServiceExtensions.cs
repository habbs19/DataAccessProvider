using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessProvider.Core.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderCore(this IServiceCollection service,IConfiguration configuration)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddScoped<IDataSourceFactory, DataSourceFactory>();

        // Add database source services

        //string mssqlString = configuration.GetConnectionString(nameof(MSSQLSource)) ?? "";
        //string postgresString = configuration.GetConnectionString(nameof(PostgresSource)) ?? "";
        //string mysqlString = configuration.GetConnectionString(nameof(MySQLSource)) ?? "";
        //string oracleString = configuration.GetConnectionString(nameof(OracleSource)) ?? "";

        //service.AddScoped<IDataSource<MSSQLSourceParams>, MSSQLSource>(provider => new MSSQLSource(mssqlString));
        //service.AddScoped<IDataSource<PostgresSourceParams>, PostgresSource>(provider => new PostgresSource(postgresString));
        //service.AddScoped<IDataSource<MySQLSourceParams>, MySQLSource>((provider) => new MySQLSource(mysqlString));
        //service.AddScoped<IDataSource<OracleSourceParams>, OracleSource>((provider) => new OracleSource(oracleString));

        //service.AddScoped(factory => new MSSQLSource(mssqlString));
        //service.AddScoped(factory => new PostgresSource(postgresString));
        //service.AddScoped(factory => new MySQLSource(mysqlString));
        //service.AddScoped(factory => new OracleSource(oracleString));

        service.AddScoped<JsonFileSource>();
        service.AddScoped<StaticCodeSource>();

        return service;
    }
}

