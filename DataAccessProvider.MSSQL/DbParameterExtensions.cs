using DataAccessProvider.Core.Types;

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

    public static MSSQLSourceParams<TValue> AddParameter<TValue>(this MSSQLSourceParams<TValue> sourceParams, string parameterName, DataAccessDbType dbType,
      object value, DataAccessParameterDirection direction = DataAccessParameterDirection.Input, int size = -1) where TValue : class
    {
        var parameters = sourceParams.Parameters ?? new List<DataAccessParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        sourceParams.Parameters = parameters;
        return sourceParams;
    }

}
