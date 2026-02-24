using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Types;
using MySqlConnector;

namespace DataAccessProvider.MySql;

internal sealed class MySqlDbTypeMapper : IDataAccessDbTypeMapper
{
    internal static MySqlDbTypeMapper Instance { get; } = new();

    private MySqlDbTypeMapper() { }

    public object Map(DataAccessDbType dbType) => dbType switch
    {
        DataAccessDbType.AnsiString => MySqlDbType.VarChar,
        DataAccessDbType.VarChar => MySqlDbType.VarChar,
        DataAccessDbType.AnsiStringFixedLength => MySqlDbType.String,
        DataAccessDbType.Char => MySqlDbType.String,
        DataAccessDbType.Binary => MySqlDbType.Blob,
        DataAccessDbType.Blob => MySqlDbType.Blob,
        DataAccessDbType.Byte => MySqlDbType.UByte,
        DataAccessDbType.TinyInt => MySqlDbType.Byte,
        DataAccessDbType.Boolean => MySqlDbType.Bool,
        DataAccessDbType.Currency => MySqlDbType.Decimal,
        DataAccessDbType.Money => MySqlDbType.Decimal,
        DataAccessDbType.Date => MySqlDbType.Date,
        DataAccessDbType.DateTime => MySqlDbType.DateTime,
        DataAccessDbType.DateTime2 => MySqlDbType.DateTime,
        DataAccessDbType.DateTimeOffset => MySqlDbType.Timestamp,
        DataAccessDbType.DateTimeTz => MySqlDbType.Timestamp,
        DataAccessDbType.Timestamp => MySqlDbType.Timestamp,
        DataAccessDbType.Decimal => MySqlDbType.Decimal,
        DataAccessDbType.Double => MySqlDbType.Double,
        DataAccessDbType.Float => MySqlDbType.Double,
        DataAccessDbType.Guid => MySqlDbType.Guid,
        DataAccessDbType.UniqueIdentifier => MySqlDbType.Guid,
        DataAccessDbType.Int16 => MySqlDbType.Int16,
        DataAccessDbType.SmallInt => MySqlDbType.Int16,
        DataAccessDbType.Int32 => MySqlDbType.Int32,
        DataAccessDbType.Int => MySqlDbType.Int32,
        DataAccessDbType.Int64 => MySqlDbType.Int64,
        DataAccessDbType.BigInt => MySqlDbType.Int64,
        DataAccessDbType.Json => MySqlDbType.JSON,
        DataAccessDbType.Object => MySqlDbType.Blob,
        DataAccessDbType.SByte => MySqlDbType.Byte,
        DataAccessDbType.Single => MySqlDbType.Float,
        DataAccessDbType.String => MySqlDbType.VarChar,
        DataAccessDbType.NVarChar => MySqlDbType.VarChar,
        DataAccessDbType.StringFixedLength => MySqlDbType.String,
        DataAccessDbType.NChar => MySqlDbType.String,
        DataAccessDbType.Text => MySqlDbType.Text,
        DataAccessDbType.Time => MySqlDbType.Time,
        DataAccessDbType.UInt16 => MySqlDbType.UInt16,
        DataAccessDbType.UInt32 => MySqlDbType.UInt32,
        DataAccessDbType.UInt64 => MySqlDbType.UInt64,
        DataAccessDbType.VarNumeric => MySqlDbType.Decimal,
        DataAccessDbType.Xml => MySqlDbType.Text,
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported MySQL data type.")
    };
}
