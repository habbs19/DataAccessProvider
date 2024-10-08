using DataAccessProvider.DataSource.Params;
using DataAccessProvider.DataSource.Source;
using DataAccessProvider.DataSource;
using DataAccessProvider.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Data.SqlTypes;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace DataAccessProvider.Extensions;
public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProvider(this IServiceCollection service,IConfiguration configuration)
    {
        // Register necessary services
        service.AddSingleton<IDataSourceProvider, DataSourceProvider>();
        service.AddSingleton(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.AddSingleton<IDataSourceFactory, DataSourceFactory>();

        // Add database source services

        string mssqlString = configuration.GetConnectionString(nameof(MSSQLSource)) ?? "";
        string postgresString = configuration.GetConnectionString(nameof(PostgresSource)) ?? "";
        string mysqlString = configuration.GetConnectionString(nameof(MySQLSource)) ?? "";
        string oracleString = configuration.GetConnectionString(nameof(OracleSource)) ?? "";

        service.AddScoped<IDataSource<MSSQLSourceParams>, MSSQLSource>(provider => new MSSQLSource(mssqlString));
        service.AddScoped<IDataSource<PostgresSourceParams>, PostgresSource>(provider => new PostgresSource(postgresString));
        service.AddScoped<IDataSource<MySQLSourceParams>, MySQLSource>((provider) => new MySQLSource(mysqlString));
        service.AddScoped<IDataSource<OracleSourceParams>, OracleSource>((provider) => new OracleSource(oracleString));

        service.AddScoped(factory => new MSSQLSource(mssqlString));
        service.AddScoped(factory => new PostgresSource(postgresString));
        service.AddScoped(factory => new MySQLSource(mysqlString));
        service.AddScoped(factory => new OracleSource(oracleString));

        service.AddScoped<JsonFileSource>();
        service.AddScoped<StaticCodeSource>();

        return service;
    }
}

