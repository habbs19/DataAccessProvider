using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Types;
using NpgsqlTypes;

namespace DataAccessProvider.Postgres;

internal sealed class PostgresDbTypeMapper : IDataAccessDbTypeMapper
{
    internal static PostgresDbTypeMapper Instance { get; } = new();

    private PostgresDbTypeMapper() { }

    public object Map(DataAccessDbType dbType) => dbType switch
    {
        DataAccessDbType.AnsiString => NpgsqlDbType.Varchar,
        DataAccessDbType.VarChar => NpgsqlDbType.Varchar,
        DataAccessDbType.AnsiStringFixedLength => NpgsqlDbType.Char,
        DataAccessDbType.Char => NpgsqlDbType.Char,
        DataAccessDbType.Binary => NpgsqlDbType.Bytea,
        DataAccessDbType.Blob => NpgsqlDbType.Bytea,
        DataAccessDbType.Byte => NpgsqlDbType.Smallint,
        DataAccessDbType.TinyInt => NpgsqlDbType.Smallint,
        DataAccessDbType.Boolean => NpgsqlDbType.Boolean,
        DataAccessDbType.Currency => NpgsqlDbType.Money,
        DataAccessDbType.Money => NpgsqlDbType.Money,
        DataAccessDbType.Date => NpgsqlDbType.Date,
        DataAccessDbType.DateTime => NpgsqlDbType.Timestamp,
        DataAccessDbType.DateTime2 => NpgsqlDbType.Timestamp,
        DataAccessDbType.DateTimeOffset => NpgsqlDbType.TimestampTz,
        DataAccessDbType.DateTimeTz => NpgsqlDbType.TimestampTz,
        DataAccessDbType.Timestamp => NpgsqlDbType.Timestamp,
        DataAccessDbType.Decimal => NpgsqlDbType.Numeric,
        DataAccessDbType.Double => NpgsqlDbType.Double,
        DataAccessDbType.Float => NpgsqlDbType.Double,
        DataAccessDbType.Guid => NpgsqlDbType.Uuid,
        DataAccessDbType.UniqueIdentifier => NpgsqlDbType.Uuid,
        DataAccessDbType.Int16 => NpgsqlDbType.Smallint,
        DataAccessDbType.SmallInt => NpgsqlDbType.Smallint,
        DataAccessDbType.Int32 => NpgsqlDbType.Integer,
        DataAccessDbType.Int => NpgsqlDbType.Integer,
        DataAccessDbType.Int64 => NpgsqlDbType.Bigint,
        DataAccessDbType.BigInt => NpgsqlDbType.Bigint,
        DataAccessDbType.Json => NpgsqlDbType.Jsonb,
        DataAccessDbType.Object => NpgsqlDbType.Jsonb,
        DataAccessDbType.SByte => NpgsqlDbType.Smallint,
        DataAccessDbType.Single => NpgsqlDbType.Real,
        DataAccessDbType.String => NpgsqlDbType.Text,
        DataAccessDbType.NVarChar => NpgsqlDbType.Text,
        DataAccessDbType.StringFixedLength => NpgsqlDbType.Char,
        DataAccessDbType.NChar => NpgsqlDbType.Char,
        DataAccessDbType.Text => NpgsqlDbType.Text,
        DataAccessDbType.Time => NpgsqlDbType.Time,
        DataAccessDbType.UInt16 => NpgsqlDbType.Integer,
        DataAccessDbType.UInt32 => NpgsqlDbType.Bigint,
        DataAccessDbType.UInt64 => NpgsqlDbType.Numeric,
        DataAccessDbType.VarNumeric => NpgsqlDbType.Numeric,
        DataAccessDbType.Xml => NpgsqlDbType.Xml,
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported Postgres data type.")
    };
}
