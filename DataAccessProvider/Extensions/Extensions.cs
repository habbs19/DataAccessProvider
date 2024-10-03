using System.Data.Common;
using System.Data;
using Microsoft.Extensions.DependencyInjection;
using DataAccessProvider.DataSource;
using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using NpgsqlTypes;
using Npgsql;
using DataAccessProvider.Types;

namespace DataAccessProvider.Extensions;
public static class Extensions
{
    /// <summary>
    /// Adds a new NpgsqlParameter to the given list of parameters.
    /// </summary>
    /// <param name="parameters">The list of NpgsqlParameters to which the new parameter will be added.</param>
    /// <param name="parameterName">The name of the parameter (e.g., @ParameterName).</param>
    /// <param name="dbType">The NpgsqlDbType representing the type of the parameter (e.g., NpgsqlDbType.Varchar).</param>
    /// <param name="value">The value of the parameter to be passed to the query or command.</param>
    /// <param name="size">Optional size of the parameter (useful for string types); defaults to -1, which indicates no specific size.</param>
    /// <returns>The updated list of NpgsqlParameters with the new parameter added.</returns>
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

    /// <summary>
    /// Adds a new SqlParameter to the given list of parameters.
    /// </summary>
    /// <param name="parameters">The list of SqlParameters to which the new parameter will be added.</param>
    /// <param name="parameterName">The name of the parameter (e.g., @ParameterName).</param>
    /// <param name="dbType">The SqlDbType representing the type of the parameter (e.g., SqlDbType.VarChar).</param>
    /// <param name="value">The value of the parameter to be passed to the query or command.</param>
    /// <param name="size">Optional size of the parameter (useful for string types); defaults to -1, which indicates no specific size.</param>
    /// <returns>The updated list of SqlParameters with the new parameter added.</returns>
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
                service.AddTransient<IJsonFileSource>(factory => new JsonFileSource());
                break;
            case ServiceLifetime.Singleton:
                service.AddSingleton<IJsonFileSource>(factory => new JsonFileSource());
                break;
            case ServiceLifetime.Scoped:
                service.AddScoped<IJsonFileSource>(factory => new JsonFileSource());
                break;
            default:
                throw new ArgumentNullException(nameof(serviceLifetime));
        }

        return service;
    }
}

