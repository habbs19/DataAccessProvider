using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Oracle;

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

}
