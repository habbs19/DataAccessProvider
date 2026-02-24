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
