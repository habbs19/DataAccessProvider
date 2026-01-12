using DataAccessProvider.Core.Types;
using Npgsql;
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
    /// <param name="dbType">The DataAccessDbType representing the type of the parameter (e.g., DataAccessDbType.String).</param>
    /// <param name="value">The value of the parameter to be passed to the query or command.</param>
    /// <param name="size">Optional size of the parameter (useful for string types); defaults to -1, which indicates no specific size.</param>
    /// <returns>The updated list of NpgsqlParameters with the new parameter added.</returns>
    public static List<NpgsqlParameter> AddParameter(this List<NpgsqlParameter> parameters, string parameterName, DataAccessDbType dbType,
        object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new NpgsqlParameter();
        parameter.ParameterName = parameterName;
        parameter.NpgsqlDbType = MapDbType(dbType);
        parameter.Value = value ?? DBNull.Value;
        parameter.Size = size;
        parameter.Direction = MapDirection(direction);

        parameters.Add(parameter);
        return parameters;
    }

    private static NpgsqlDbType MapDbType(DataAccessDbType dbType) => dbType switch
    {
        DataAccessDbType.AnsiString => NpgsqlDbType.Varchar,
        DataAccessDbType.AnsiStringFixedLength => NpgsqlDbType.Char,
        DataAccessDbType.Binary => NpgsqlDbType.Bytea,
        DataAccessDbType.Byte => NpgsqlDbType.Smallint,
        DataAccessDbType.Boolean => NpgsqlDbType.Boolean,
        DataAccessDbType.Currency => NpgsqlDbType.Money,
        DataAccessDbType.Date => NpgsqlDbType.Date,
        DataAccessDbType.DateTime => NpgsqlDbType.Timestamp,
        DataAccessDbType.DateTime2 => NpgsqlDbType.Timestamp,
        DataAccessDbType.DateTimeOffset => NpgsqlDbType.TimestampTz,
        DataAccessDbType.Decimal => NpgsqlDbType.Numeric,
        DataAccessDbType.Double => NpgsqlDbType.Double,
        DataAccessDbType.Guid => NpgsqlDbType.Uuid,
        DataAccessDbType.Int16 => NpgsqlDbType.Smallint,
        DataAccessDbType.Int32 => NpgsqlDbType.Integer,
        DataAccessDbType.Int64 => NpgsqlDbType.Bigint,
        DataAccessDbType.Json => NpgsqlDbType.Jsonb,
        DataAccessDbType.Object => NpgsqlDbType.Jsonb,
        DataAccessDbType.SByte => NpgsqlDbType.Smallint,
        DataAccessDbType.Single => NpgsqlDbType.Real,
        DataAccessDbType.String => NpgsqlDbType.Text,
        DataAccessDbType.StringFixedLength => NpgsqlDbType.Char,
        DataAccessDbType.Time => NpgsqlDbType.Time,
        DataAccessDbType.UInt16 => NpgsqlDbType.Integer,
        DataAccessDbType.UInt32 => NpgsqlDbType.Bigint,
        DataAccessDbType.UInt64 => NpgsqlDbType.Numeric,
        DataAccessDbType.VarNumeric => NpgsqlDbType.Numeric,
        DataAccessDbType.Xml => NpgsqlDbType.Xml,
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported Postgres data type.")
    };

    private static ParameterDirection MapDirection(DataAccessParameterDirection direction) => direction switch
    {
        DataAccessParameterDirection.Input => ParameterDirection.Input,
        DataAccessParameterDirection.Output => ParameterDirection.Output,
        DataAccessParameterDirection.InputOutput => ParameterDirection.InputOutput,
        DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported parameter direction.")
    };
}
