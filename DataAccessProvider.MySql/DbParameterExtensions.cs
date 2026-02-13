using DataAccessProvider.Core.Types;
using MySqlConnector;
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

    internal static MySqlDbType MapDbType(DataAccessDbType dbType) => dbType switch
    {
        DataAccessDbType.AnsiString => MySqlDbType.VarChar,
        DataAccessDbType.AnsiStringFixedLength => MySqlDbType.String,
        DataAccessDbType.Binary => MySqlDbType.Blob,
        DataAccessDbType.Byte => MySqlDbType.UByte,
        DataAccessDbType.Boolean => MySqlDbType.Bool,
        DataAccessDbType.Currency => MySqlDbType.Decimal,
        DataAccessDbType.Date => MySqlDbType.Date,
        DataAccessDbType.DateTime => MySqlDbType.DateTime,
        DataAccessDbType.DateTime2 => MySqlDbType.DateTime,
        DataAccessDbType.DateTimeOffset => MySqlDbType.Timestamp,
        DataAccessDbType.Decimal => MySqlDbType.Decimal,
        DataAccessDbType.Double => MySqlDbType.Double,
        DataAccessDbType.Guid => MySqlDbType.Guid,
        DataAccessDbType.Int16 => MySqlDbType.Int16,
        DataAccessDbType.Int32 => MySqlDbType.Int32,
        DataAccessDbType.Int64 => MySqlDbType.Int64,
        DataAccessDbType.Json => MySqlDbType.JSON,
        DataAccessDbType.Object => MySqlDbType.Blob,
        DataAccessDbType.SByte => MySqlDbType.Byte,
        DataAccessDbType.Single => MySqlDbType.Float,
        DataAccessDbType.String => MySqlDbType.VarChar,
        DataAccessDbType.StringFixedLength => MySqlDbType.String,
        DataAccessDbType.Time => MySqlDbType.Time,
        DataAccessDbType.UInt16 => MySqlDbType.UInt16,
        DataAccessDbType.UInt32 => MySqlDbType.UInt32,
        DataAccessDbType.UInt64 => MySqlDbType.UInt64,
        DataAccessDbType.VarNumeric => MySqlDbType.Decimal,
        DataAccessDbType.Xml => MySqlDbType.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported MySQL data type.")
    };
}
