using Snowflake.Data.Client;
using Snowflake.Data.Core;
using System.Data;

namespace DataAccessProvider.Snowflake;

public static class DbParameterExtensions
{   
    public static List<SnowflakeDbParameter> AddParameter(this List<SnowflakeDbParameter> parameters, string parameterName, SFDataType dbType,
        object value, ParameterDirection direction = ParameterDirection.Input, int size = 0)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new SnowflakeDbParameter();
        parameter.ParameterName = parameterName;
        parameter.SFDataType = dbType;
        parameter.Value = value ?? DBNull.Value;
        parameter.Size = size;
        parameter.Direction = direction;

        parameters.Add(parameter);
        return parameters;
    }
}
