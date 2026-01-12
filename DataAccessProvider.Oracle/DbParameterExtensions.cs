using DataAccessProvider.Core.Types;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DataAccessProvider.Oracle;

public static class DbParameterExtensions
{
    public static List<OracleParameter> AddParameter(this List<OracleParameter> parameters, string parameterName, DataAccessDbType dbType,
        object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new OracleParameter();
        parameter.ParameterName = parameterName;
        parameter.OracleDbType = MapDbType(dbType);
        parameter.Value = value ?? DBNull.Value;
        parameter.Size = size;
        parameter.Direction = MapDirection(direction);

        parameters.Add(parameter);
        return parameters;
    }

    private static OracleDbType MapDbType(DataAccessDbType dbType) => dbType switch
    {
        DataAccessDbType.AnsiString => OracleDbType.Varchar2,
        DataAccessDbType.AnsiStringFixedLength => OracleDbType.Char,
        DataAccessDbType.Binary => OracleDbType.Blob,
        DataAccessDbType.Byte => OracleDbType.Byte,
        DataAccessDbType.Boolean => OracleDbType.Boolean,
        DataAccessDbType.Currency => OracleDbType.Decimal,
        DataAccessDbType.Date => OracleDbType.Date,
        DataAccessDbType.DateTime => OracleDbType.TimeStamp,
        DataAccessDbType.DateTime2 => OracleDbType.TimeStamp,
        DataAccessDbType.DateTimeOffset => OracleDbType.TimeStampTZ,
        DataAccessDbType.Decimal => OracleDbType.Decimal,
        DataAccessDbType.Double => OracleDbType.Double,
        DataAccessDbType.Guid => OracleDbType.Raw,
        DataAccessDbType.Int16 => OracleDbType.Int16,
        DataAccessDbType.Int32 => OracleDbType.Int32,
        DataAccessDbType.Int64 => OracleDbType.Int64,
        DataAccessDbType.Json => OracleDbType.Json,
        DataAccessDbType.Object => OracleDbType.Blob,
        DataAccessDbType.SByte => OracleDbType.Int16,
        DataAccessDbType.Single => OracleDbType.Single,
        DataAccessDbType.String => OracleDbType.NVarchar2,
        DataAccessDbType.StringFixedLength => OracleDbType.NChar,
        DataAccessDbType.Time => OracleDbType.TimeStamp,
        DataAccessDbType.UInt16 => OracleDbType.Int32,
        DataAccessDbType.UInt32 => OracleDbType.Int64,
        DataAccessDbType.UInt64 => OracleDbType.Decimal,
        DataAccessDbType.VarNumeric => OracleDbType.Decimal,
        DataAccessDbType.Xml => OracleDbType.XmlType,
        _ => throw new ArgumentOutOfRangeException(nameof(dbType), dbType, "Unsupported Oracle data type.")
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
