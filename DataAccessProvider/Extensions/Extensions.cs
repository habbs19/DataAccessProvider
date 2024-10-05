using System.Data.Common;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using DataAccessProvider.DataSource;
using Microsoft.Data.SqlClient;
using NpgsqlTypes;
using Npgsql;
using DataAccessProvider.Types;

namespace DataAccessProvider.Extensions;
public static class Extensions
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
}

