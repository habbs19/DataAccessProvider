using DataAccessProvider.Core.Types;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataAccessProvider.MySql;

public static class DbParameterExtensions
{
    public static List<DataAccessParameter> AddParameter(this List<DataAccessParameter> parameters, string parameterName, DataAccessDbType dbType,
        object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1)
    {
        parameters.Add(new DataAccessParameter
        {
            ParameterName = parameterName,
            DbType = dbType,
            Value = value,
            Direction = direction,
            Size = size
        });

        return parameters;
    }

    public static MySQLSourceParams AddParameter(this MySQLSourceParams sourceParams, string parameterName, DataAccessDbType dbType,
       object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1)
    {
        var parameters = sourceParams.Parameters ?? new List<DataAccessParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        sourceParams.Parameters = parameters;
        return sourceParams;
    }

    public static MySQLSourceParams<TValue> AddParameter<TValue>(this MySQLSourceParams<TValue> sourceParams, string parameterName, DataAccessDbType dbType,
      object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1) where TValue : class
    {
        var parameters = sourceParams.Parameters ?? new List<DataAccessParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        sourceParams.Parameters = parameters;
        return sourceParams;
    }

    public static MySQLSourceParams AddJSONParams(this MySQLSourceParams sourceParams, int operation, object? json = null!)
    {
        var parameters = sourceParams.Parameters ?? new List<DataAccessParameter>();
        parameters.AddParameter("Operation", DataAccessDbType.UInt16, operation);

        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string? jsonPayload = json == null ? null : JsonSerializer.Serialize(json, options);

        parameters.AddParameter("Params", DataAccessDbType.Json, jsonPayload!);
        sourceParams.Parameters = parameters;
        return sourceParams;
    }

    public static MySQLSourceParams<TValue> AddJSONParams<TValue>(this MySQLSourceParams<TValue> sourceParams, int operation, object? json = null!) where TValue : class
    {
        var parameters = sourceParams.Parameters ?? new List<DataAccessParameter>();
        parameters.AddParameter("Operation", DataAccessDbType.UInt16, operation);
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        string? jsonPayload = json == null ? null : JsonSerializer.Serialize(json, options);

        parameters.AddParameter("Params", DataAccessDbType.Json, jsonPayload!);
        sourceParams.Parameters = parameters;
        return sourceParams;
    }

}
