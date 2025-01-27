using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace DataAccessProvider.Oracle;

public static class DbParameterExtensions
{
    public static List<OracleParameter> AddParameter(this List<OracleParameter> parameters, string parameterName, OracleDbType dbType,
        object value, ParameterDirection direction = ParameterDirection.Input,  int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new OracleParameter();
        parameter.ParameterName = parameterName;
        parameter.OracleDbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        parameter.Size = size;
        parameter.Direction = direction;

        parameters.Add(parameter);
        return parameters;
    }
}
