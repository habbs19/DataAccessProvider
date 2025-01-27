using MySql.Data.MySqlClient;
using System.Data;

namespace DataAccessProvider.MySql;

public static class DbParameterExtensions
{   
    public static List<MySqlParameter> AddParameter(this List<MySqlParameter> parameters, string parameterName, MySqlDbType dbType,
        object value, ParameterDirection direction = ParameterDirection.Input, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new MySqlParameter();
        parameter.ParameterName = parameterName;
        parameter.MySqlDbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        parameter.Direction = direction;

        if (size > 0)
        {
            parameter.Size = size;
        }

        parameters.Add(parameter);
        return parameters;
    }

    public static MySQLSourceParams AddParameter(this MySQLSourceParams sourceParams, string parameterName, MySqlDbType dbType,
       object value, ParameterDirection direction = ParameterDirection.Input, int size = -1)
    {
        var parameters = sourceParams.Parameters ?? new List<MySqlParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        return sourceParams;
    }
}
