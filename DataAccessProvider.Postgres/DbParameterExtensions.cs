using DataAccessProvider.Core.Types;
using NpgsqlTypes;

namespace DataAccessProvider.Postgres;

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

    internal static NpgsqlDbType MapDbType(DataAccessDbType dbType) => dbType switch
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
}
