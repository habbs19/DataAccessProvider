using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessProvider.MSSQL;

public static class DbParameterExtensions
{
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
        object value, ParameterDirection direction = ParameterDirection.Input, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new SqlParameter();
        parameter.ParameterName = parameterName;
        parameter.SqlDbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        parameter.Size = size;
        parameter.Direction = direction;

        parameters.Add(parameter);
        return parameters;
    }

    public static MSSQLSourceParams AddParameter(this MSSQLSourceParams sourceParams, string parameterName, SqlDbType dbType,
        object value, ParameterDirection direction = ParameterDirection.Input, int size = -1)
    {
        var parameters = sourceParams.Parameters ?? new List<SqlParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        return sourceParams;
    }
}
