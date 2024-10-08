using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Npgsql;
using NpgsqlTypes;
using Oracle.ManagedDataAccess.Client;
using Snowflake.Data.Client;
using Snowflake.Data.Core;
using System.Data;

namespace DataAccessProvider.Extensions;

public static class DbParameterExtensions
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
        object value, ParameterDirection direction = ParameterDirection.Input,int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new NpgsqlParameter();
        parameter.ParameterName = parameterName;
        parameter.NpgsqlDbType = dbType;
        parameter.Value = value;
        parameter.Size = size;
        parameter.Direction = direction;

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
        object value, ParameterDirection direction = ParameterDirection.Input,int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new SqlParameter();
        parameter.ParameterName = parameterName;
        parameter.SqlDbType = dbType;
        parameter.Value = value;
        parameter.Size = size;
        parameter.Direction = direction;

        parameters.Add(parameter);
        return parameters;
    }

    public static List<OracleParameter> AddParameter(this List<OracleParameter> parameters, string parameterName, OracleDbType dbType,
        object value, ParameterDirection direction = ParameterDirection.Input,  int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new OracleParameter();
        parameter.ParameterName = parameterName;
        parameter.OracleDbType = dbType;
        parameter.Value = value;
        parameter.Size = size;
        parameter.Direction = direction;

        parameters.Add(parameter);
        return parameters;
    }

    public static List<MySqlParameter> AddParameter(this List<MySqlParameter> parameters, string parameterName, MySqlDbType dbType,
        object value, ParameterDirection direction = ParameterDirection.Input, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new MySqlParameter();
        parameter.ParameterName = parameterName;
        parameter.MySqlDbType = dbType;
        parameter.Value = value;
        parameter.Size = size;
        parameter.Direction = direction;

        parameters.Add(parameter);
        return parameters;
    }

    public static List<SnowflakeDbParameter> AddParameter(this List<SnowflakeDbParameter> parameters, string parameterName, SFDataType dbType,
        object value, ParameterDirection direction = ParameterDirection.Input, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new SnowflakeDbParameter();
        parameter.ParameterName = parameterName;
        parameter.SFDataType = dbType;
        parameter.Value = value;
        parameter.Size = size;
        parameter.Direction = direction;

        parameters.Add(parameter);
        return parameters;
    }
}
