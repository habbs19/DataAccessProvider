using DataAccessProvider.Core.Types;
using Snowflake.Data.Client;
using Snowflake.Data.Core;
using System.Data;

namespace DataAccessProvider.Snowflake;

public static class DbParameterExtensions
{   
    public static List<SnowflakeDbParameter> AddParameter(this List<SnowflakeDbParameter> parameters, string parameterName, DataAccessDbType dbType,
        object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = 0)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new SnowflakeDbParameter();
        parameter.ParameterName = parameterName;
        parameter.SFDataType = MapDbType(dbType);
        parameter.Value = value ?? DBNull.Value;
        parameter.Size = size;
        parameter.Direction = MapDirection(direction);

        parameters.Add(parameter);
        return parameters;
    }

    private static SFDataType MapDbType(DataAccessDbType dbType) => dbType switch
    {
        DataAccessDbType.AnsiString => ParseDbType("Text"),
        DataAccessDbType.AnsiStringFixedLength => ParseDbType("Text"),
        DataAccessDbType.Binary => ParseDbType("Binary"),
        DataAccessDbType.Byte => ParseDbType("Fixed"),
        DataAccessDbType.Boolean => ParseDbType("Boolean"),
        DataAccessDbType.Currency => ParseDbType("Fixed"),
        DataAccessDbType.Date => ParseDbType("Date"),
        DataAccessDbType.DateTime => ParseDbType("Timestamp"),
        DataAccessDbType.DateTime2 => ParseDbType("Timestamp"),
        DataAccessDbType.DateTimeOffset => ParseDbType("TimestampTz"),
        DataAccessDbType.Decimal => ParseDbType("Fixed"),
        DataAccessDbType.Double => ParseDbType("Real"),
        DataAccessDbType.Guid => ParseDbType("Text"),
        DataAccessDbType.Int16 => ParseDbType("Fixed"),
        DataAccessDbType.Int32 => ParseDbType("Fixed"),
        DataAccessDbType.Int64 => ParseDbType("Fixed"),
        DataAccessDbType.Json => ParseDbType("Variant"),
        DataAccessDbType.Object => ParseDbType("Object"),
        DataAccessDbType.SByte => ParseDbType("Fixed"),
        DataAccessDbType.Single => ParseDbType("Real"),
        DataAccessDbType.String => ParseDbType("Text"),
        DataAccessDbType.StringFixedLength => ParseDbType("Text"),
        DataAccessDbType.Time => ParseDbType("Time"),
        DataAccessDbType.UInt16 => ParseDbType("Fixed"),
        DataAccessDbType.UInt32 => ParseDbType("Fixed"),
        DataAccessDbType.UInt64 => ParseDbType("Fixed"),
        DataAccessDbType.VarNumeric => ParseDbType("Fixed"),
        DataAccessDbType.Xml => ParseDbType("Text"),
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported Snowflake data type.")
    };

    private static SFDataType ParseDbType(string name)
        => Enum.TryParse<SFDataType>(name, ignoreCase: true, out var parsed) ? parsed : default;

    private static ParameterDirection MapDirection(DataAccessParameterDirection direction) => direction switch
    {
        DataAccessParameterDirection.Input => ParameterDirection.Input,
        DataAccessParameterDirection.Output => ParameterDirection.Output,
        DataAccessParameterDirection.InputOutput => ParameterDirection.InputOutput,
        DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported parameter direction.")
    };
}
