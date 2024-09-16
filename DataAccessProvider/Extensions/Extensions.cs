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
        var parameter = CreateParameter<NpgsqlParameter>(parameterName, (DbType)dbType, value, size);
        parameters.Add(parameter);
        return parameters;
    }

    public static List<SqlParameter> AddParameter(this List<SqlParameter> parameters, string parameterName, SqlDbType dbType,
        object value, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = CreateParameter<SqlParameter>(parameterName, (DbType)dbType, value, size);
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

    private static TParameter CreateParameter<TParameter>(string parameterName, DbType dbType,
           object value, int size = -1) where TParameter : DbParameter, new()
    {
        var parameter = new TParameter();

        // Set common properties
        parameter.ParameterName = parameterName;
        parameter.DbType = dbType;
        parameter.Value = value;
        parameter.Size = size;

        // If needed, handle specific parameter types (e.g., SqlParameter, NpgsqlParameter)
        // This is generally not needed if you're only working with TParameter as DbParameter
        // but could be useful if there are type-specific properties to set

        return parameter;
    }
}

