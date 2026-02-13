using DataAccessProvider.Core.Types;
using Microsoft.Data.SqlClient;

namespace DataAccessProvider.MSSQL;

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

    public static MSSQLSourceParams AddParameter(this MSSQLSourceParams sourceParams, string parameterName, DataAccessDbType dbType,
        object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1)
    {
        var parameters = sourceParams.Parameters ?? new List<DataAccessParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        sourceParams.Parameters = parameters;
        return sourceParams;
    }

    internal static SqlDbType MapDbType(DataAccessDbType dbType) => dbType switch
    {
        DataAccessDbType.AnsiString => SqlDbType.VarChar,
        DataAccessDbType.AnsiStringFixedLength => SqlDbType.Char,
        DataAccessDbType.Binary => SqlDbType.VarBinary,
        DataAccessDbType.Byte => SqlDbType.TinyInt,
        DataAccessDbType.Boolean => SqlDbType.Bit,
        DataAccessDbType.Currency => SqlDbType.Money,
        DataAccessDbType.Date => SqlDbType.Date,
        DataAccessDbType.DateTime => SqlDbType.DateTime,
        DataAccessDbType.DateTime2 => SqlDbType.DateTime2,
        DataAccessDbType.DateTimeOffset => SqlDbType.DateTimeOffset,
        DataAccessDbType.Decimal => SqlDbType.Decimal,
        DataAccessDbType.Double => SqlDbType.Float,
        DataAccessDbType.Guid => SqlDbType.UniqueIdentifier,
        DataAccessDbType.Int16 => SqlDbType.SmallInt,
        DataAccessDbType.Int32 => SqlDbType.Int,
        DataAccessDbType.Int64 => SqlDbType.BigInt,
        DataAccessDbType.Json => SqlDbType.NVarChar,
        DataAccessDbType.Object => SqlDbType.Variant,
        DataAccessDbType.SByte => SqlDbType.SmallInt,
        DataAccessDbType.Single => SqlDbType.Real,
        DataAccessDbType.String => SqlDbType.NVarChar,
        DataAccessDbType.StringFixedLength => SqlDbType.NChar,
        DataAccessDbType.Time => SqlDbType.Time,
        DataAccessDbType.UInt16 => SqlDbType.Int,
        DataAccessDbType.UInt32 => SqlDbType.BigInt,
        DataAccessDbType.UInt64 => SqlDbType.Decimal,
        DataAccessDbType.VarNumeric => SqlDbType.Decimal,
        DataAccessDbType.Xml => SqlDbType.Xml,
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported MSSQL data type.")
    };
}
