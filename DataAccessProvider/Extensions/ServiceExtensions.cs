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
   

    public static IServiceCollection AddPostgresProvider(this IServiceCollection service, string connectionString, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        switch (serviceLifetime)
        {
            case ServiceLifetime.Transient:
               // service.AddTransient<IDatabasePostgres>(factory => new PostgresDatabase(connectionString));
               // service.AddTransient<IDataSource<Postgres, NpgsqlParameter>, PostgresDatabase>(factory => new PostgresDatabase(connectionString));
                break;
            case ServiceLifetime.Singleton:
             //   service.AddTransient<IDatabasePostgres>(factory => new PostgresDatabase(connectionString));
             //   service.AddSingleton<IDataSource<Postgres, NpgsqlParameter>, PostgresDatabase>(factory => new PostgresDatabase(connectionString));
                break;
            case ServiceLifetime.Scoped:
             //   service.AddTransient<IDatabasePostgres>(factory => new PostgresDatabase(connectionString));
            //    service.AddScoped<IDataSource<Postgres, NpgsqlParameter>, PostgresDatabase>(factory => new PostgresDatabase(connectionString));
                break;
            default: throw new ArgumentNullException(nameof(serviceLifetime));

        }
        return service;
    }

    public static IServiceCollection AddMSSQLProvider(this IServiceCollection service, string connectionString, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        switch (serviceLifetime)
        {
            case ServiceLifetime.Transient:
             //   service.AddTransient<IDatabaseMSSQL>(factory => new MSSQLDatabase(connectionString));
             //   service.AddTransient<IDataSource<MSSQL, SqlParameter>, MSSQLDatabase>(factory => new MSSQLDatabase(connectionString));
                break;
            case ServiceLifetime.Singleton:
             //   service.AddSingleton<IDatabaseMSSQL>(factory => new MSSQLDatabase(connectionString));
             //   service.AddSingleton<IDataSource<MSSQL, SqlParameter>, MSSQLDatabase>(factory => new MSSQLDatabase(connectionString));
                break;
            case ServiceLifetime.Scoped:
            //    service.AddScoped<IDatabaseMSSQL>(factory => new MSSQLDatabase(connectionString));
             //   service.AddScoped<IDataSource<MSSQL, SqlParameter>, MSSQLDatabase>(factory => new MSSQLDatabase(connectionString));
                break;
            default: throw new ArgumentNullException(nameof(serviceLifetime));

        }
        return service;
    }

    public static IServiceCollection AddJsonFileProvider(this IServiceCollection service, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        switch (serviceLifetime)
        {
            case ServiceLifetime.Transient:
                //service.AddTransient<IJsonFileSource>(factory => new JsonFileSource());
                break;
            case ServiceLifetime.Singleton:
                //service.AddSingleton<IJsonFileSource>(factory => new JsonFileSource());
                break;
            case ServiceLifetime.Scoped:
                //service.AddScoped<IJsonFileSource>(factory => new JsonFileSource());
                break;
            default:
                throw new ArgumentNullException(nameof(serviceLifetime));
        }

        return service;
    }

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

