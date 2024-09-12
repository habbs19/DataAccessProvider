using System.Data.Common;
using System.Data;

namespace DataAccessProvider.Extensions;
public static class Extensions
{
    public static List<TParameter> AddParameter<TParameter>(this List<TParameter> parameters, string parameterName, DbType dbType,
        object value, int size = -1) where TParameter : DbParameter, new()
    {
        // Create the appropriate parameter and add it to the list
        var parameter = CreateParameter<TParameter>(parameterName, dbType, value, size);
        parameters.Add(parameter);
        return parameters;
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

