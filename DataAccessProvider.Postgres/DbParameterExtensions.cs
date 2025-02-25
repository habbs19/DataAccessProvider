﻿using Npgsql;
using NpgsqlTypes;
using System.Data;

namespace DataAccessProvider.Postgres;

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
        parameter.Value = value ?? DBNull.Value;
        parameter.Size = size;
        parameter.Direction = direction;

        parameters.Add(parameter);
        return parameters;
    }   
}
