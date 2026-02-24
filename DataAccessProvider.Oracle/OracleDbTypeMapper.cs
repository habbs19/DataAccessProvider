using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Types;
using Oracle.ManagedDataAccess.Client;

namespace DataAccessProvider.Oracle;

internal sealed class OracleDbTypeMapper : IDataAccessDbTypeMapper
{
    internal static OracleDbTypeMapper Instance { get; } = new();

    private OracleDbTypeMapper() { }

    public object Map(DataAccessDbType dbType) => dbType switch
    {
        DataAccessDbType.AnsiString => OracleDbType.Varchar2,
        DataAccessDbType.VarChar => OracleDbType.Varchar2,
        DataAccessDbType.AnsiStringFixedLength => OracleDbType.Char,
        DataAccessDbType.Char => OracleDbType.Char,
        DataAccessDbType.Binary => OracleDbType.Blob,
        DataAccessDbType.Blob => OracleDbType.Blob,
        DataAccessDbType.Byte => OracleDbType.Byte,
        DataAccessDbType.TinyInt => OracleDbType.Byte,
        DataAccessDbType.Boolean => OracleDbType.Boolean,
        DataAccessDbType.Currency => OracleDbType.Decimal,
        DataAccessDbType.Money => OracleDbType.Decimal,
        DataAccessDbType.Date => OracleDbType.Date,
        DataAccessDbType.DateTime => OracleDbType.TimeStamp,
        DataAccessDbType.DateTime2 => OracleDbType.TimeStamp,
        DataAccessDbType.DateTimeOffset => OracleDbType.TimeStampTZ,
        DataAccessDbType.DateTimeTz => OracleDbType.TimeStampTZ,
        DataAccessDbType.Timestamp => OracleDbType.TimeStamp,
        DataAccessDbType.Decimal => OracleDbType.Decimal,
        DataAccessDbType.Double => OracleDbType.Double,
        DataAccessDbType.Float => OracleDbType.Double,
        DataAccessDbType.Guid => OracleDbType.Raw,
        DataAccessDbType.UniqueIdentifier => OracleDbType.Raw,
        DataAccessDbType.Int16 => OracleDbType.Int16,
        DataAccessDbType.SmallInt => OracleDbType.Int16,
        DataAccessDbType.Int32 => OracleDbType.Int32,
        DataAccessDbType.Int => OracleDbType.Int32,
        DataAccessDbType.Int64 => OracleDbType.Int64,
        DataAccessDbType.BigInt => OracleDbType.Int64,
        DataAccessDbType.Json => OracleDbType.Json,
        DataAccessDbType.Object => OracleDbType.Blob,
        DataAccessDbType.SByte => OracleDbType.Int16,
        DataAccessDbType.Single => OracleDbType.Single,
        DataAccessDbType.String => OracleDbType.NVarchar2,
        DataAccessDbType.NVarChar => OracleDbType.NVarchar2,
        DataAccessDbType.StringFixedLength => OracleDbType.NChar,
        DataAccessDbType.NChar => OracleDbType.NChar,
        DataAccessDbType.Text => OracleDbType.NClob,
        DataAccessDbType.Time => OracleDbType.TimeStamp,
        DataAccessDbType.UInt16 => OracleDbType.Int32,
        DataAccessDbType.UInt32 => OracleDbType.Int64,
        DataAccessDbType.UInt64 => OracleDbType.Decimal,
        DataAccessDbType.VarNumeric => OracleDbType.Decimal,
        DataAccessDbType.Xml => OracleDbType.XmlType,
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported Oracle data type.")
    };
}
