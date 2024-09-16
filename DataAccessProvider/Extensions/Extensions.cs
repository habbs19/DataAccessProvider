using System.Data.Common;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using DataAccessProvider.Database;
using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using NpgsqlTypes;
using Npgsql;

namespace DataAccessProvider.Extensions;
public static class Extensions
{
    public static List<NpgsqlParameter> AddParameter(this List<NpgsqlParameter> parameters, string parameterName, NpgsqlDbType dbType,
        object value, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new NpgsqlParameter();
        parameter.ParameterName = parameterName;
        parameter.NpgsqlDbType = dbType;
        parameter.Value = value;
        parameter.Size = size;

        parameters.Add(parameter);
        return parameters;
    }

    public static List<SqlParameter> AddParameter(this List<SqlParameter> parameters, string parameterName, SqlDbType dbType,
        object value, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new SqlParameter();
        parameter.ParameterName = parameterName;
        parameter.SqlDbType = dbType;
        parameter.Value = value;
        parameter.Size = size;

        parameters.Add(parameter);
        return parameters;
    }

    public static IServiceCollection AddPostgresProvider(this IServiceCollection service, string connectionString, ServiceLifetime serviceLifetime = ServiceLifetime.Transient)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        switch (serviceLifetime)
        {
            case ServiceLifetime.Transient:
                service.AddTransient<IDatabasePostgres>(factory => new PostgresDatabase(connectionString));
                service.AddTransient<IDatabase<Postgres, NpgsqlParameter>, PostgresDatabase>(factory => new PostgresDatabase(connectionString));
                break;
            case ServiceLifetime.Singleton:
                service.AddTransient<IDatabasePostgres>(factory => new PostgresDatabase(connectionString));
                service.AddSingleton<IDatabase<Postgres, NpgsqlParameter>, PostgresDatabase>(factory => new PostgresDatabase(connectionString));
                break;
            case ServiceLifetime.Scoped:
                service.AddTransient<IDatabasePostgres>(factory => new PostgresDatabase(connectionString));
                service.AddScoped<IDatabase<Postgres, NpgsqlParameter>, PostgresDatabase>(factory => new PostgresDatabase(connectionString));
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
                service.AddTransient<IDatabaseMSSQL>(factory => new MSSQLDatabase(connectionString));
                service.AddTransient<IDatabase<MSSQL, SqlParameter>, MSSQLDatabase>(factory => new MSSQLDatabase(connectionString));
                break;
            case ServiceLifetime.Singleton:
                service.AddSingleton<IDatabaseMSSQL>(factory => new MSSQLDatabase(connectionString));
                service.AddSingleton<IDatabase<MSSQL, SqlParameter>, MSSQLDatabase>(factory => new MSSQLDatabase(connectionString));
                break;
            case ServiceLifetime.Scoped:
                service.AddScoped<IDatabaseMSSQL>(factory => new MSSQLDatabase(connectionString));
                service.AddScoped<IDatabase<MSSQL, SqlParameter>, MSSQLDatabase>(factory => new MSSQLDatabase(connectionString));
                break;
            default: throw new ArgumentNullException(nameof(serviceLifetime));

        }
        return service;
    }
}

