using DataAccessProvider.Core.Types;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessProvider.MSSQL;

public static class DbParameterExtensions
{
    /// <summary>
    /// Adds a new SqlParameter to the given list of parameters.
    /// </summary>
    /// <param name="parameters">The list of SqlParameters to which the new parameter will be added.</param>
    /// <param name="parameterName">The name of the parameter (e.g., @ParameterName).</param>
    /// <param name="dbType">The DataAccessDbType representing the type of the parameter (e.g., DataAccessDbType.String).</param>
    /// <param name="value">The value of the parameter to be passed to the query or command.</param>
    /// <param name="size">Optional size of the parameter (useful for string types); defaults to -1, which indicates no specific size.</param>
    /// <returns>The updated list of SqlParameters with the new parameter added.</returns>
    public static List<SqlParameter> AddParameter(this List<SqlParameter> parameters, string parameterName, DataAccessDbType dbType,
        object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new SqlParameter();
        parameter.ParameterName = parameterName;
        parameter.SqlDbType = MapDbType(dbType);
        parameter.Value = value ?? DBNull.Value;
        parameter.Size = size;
        parameter.Direction = MapDirection(direction);

        parameters.Add(parameter);
        return parameters;
    }

    public static MSSQLSourceParams AddParameter(this MSSQLSourceParams sourceParams, string parameterName, DataAccessDbType dbType,
        object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1)
    {
        var parameters = sourceParams.Parameters ?? new List<SqlParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        return sourceParams;
    }

    private static SqlDbType MapDbType(DataAccessDbType dbType) => dbType switch
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

    private static ParameterDirection MapDirection(DataAccessParameterDirection direction) => direction switch
    {
        DataAccessParameterDirection.Input => ParameterDirection.Input,
        DataAccessParameterDirection.Output => ParameterDirection.Output,
        DataAccessParameterDirection.InputOutput => ParameterDirection.InputOutput,
        DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported parameter direction.")
    };
}
