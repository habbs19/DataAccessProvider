using MySql.Data.MySqlClient;
using System.Data;
using System.Text.Json;

namespace DataAccessProvider.MySql;

public static class DbParameterExtensions
{   
    public static List<MySqlParameter> AddParameter(this List<MySqlParameter> parameters, string parameterName, MySqlDbType dbType,
        object value, ParameterDirection direction = ParameterDirection.Input, int size = -1)
    {
        // Create the appropriate parameter and add it to the list
        var parameter = new MySqlParameter();
        parameter.ParameterName = parameterName;
        parameter.MySqlDbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        parameter.Direction = direction;

        if (size > 0)
        {
            parameter.Size = size;
        }

        parameters.Add(parameter);
        return parameters;
    }

    public static MySQLSourceParams AddParameter(this MySQLSourceParams sourceParams, string parameterName, MySqlDbType dbType,
       object value, ParameterDirection direction = ParameterDirection.Input, int size = -1)
    {
        var parameters = sourceParams.Parameters ?? new List<MySqlParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        return sourceParams;
    }

    public static MySQLSourceParams<TValue> AddParameter<TValue>(this MySQLSourceParams<TValue> sourceParams, string parameterName, MySqlDbType dbType,
      object value, ParameterDirection direction = ParameterDirection.Input, int size = -1) where TValue : class
    {
        var parameters = sourceParams.Parameters ?? new List<MySqlParameter>();
        parameters.AddParameter(parameterName, dbType, value, direction, size);
        return sourceParams;
    }

    /// <summary>
    /// Adds an operation code and a JSON-serialized object to the <see cref="MySQLSourceParams"/> as parameters.
    /// </summary>
    /// <param name="sourceParams">The <see cref="MySQLSourceParams"/> object to which the parameters are added.</param>
    /// <param name="operation">The operation code to be added as a parameter, typically representing a specific action or query operation.</param>
    /// <param name="json">An object that will be serialized into JSON and added as a parameter named "Params".</param>
    /// <returns>The updated <see cref="MySQLSourceParams"/> object with the added parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="sourceParams"/> or <paramref name="json"/> is null.</exception>
    public static MySQLSourceParams AddJSONParams(this MySQLSourceParams sourceParams, int operation, object? json = null!)
    {
        var parameters = sourceParams.Parameters ?? new List<MySqlParameter>();
        parameters.AddParameter("Operation",MySqlDbType.UInt16, operation);
        parameters.AddParameter("Params", MySqlDbType.JSON, JsonSerializer.Serialize(json));
        return sourceParams;
    }
    /// <summary>
    /// Adds an operation code and a JSON-serialized object to the <see cref="MySQLSourceParams{TValue}"/> as parameters.
    /// </summary>
    /// <typeparam name="TValue">The type parameter representing the value type used in <see cref="MySQLSourceParams{TValue}"/>. Must be a reference type.</typeparam>
    /// <param name="sourceParams">The <see cref="MySQLSourceParams{TValue}"/> object to which the parameters are added.</param>
    /// <param name="operation">The operation code to be added as a parameter, typically representing a specific action or query operation.</param>
    /// <param name="json">An object that will be serialized into JSON and added as a parameter named "Params".</param>
    /// <returns>The updated <see cref="MySQLSourceParams{TValue}"/> object with the added parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="sourceParams"/> or <paramref name="json"/> is null.</exception>
    public static MySQLSourceParams<TValue> AddJSONParams<TValue>(this MySQLSourceParams<TValue> sourceParams, int operation, object? json = null!) where TValue : class
    {
        var parameters = sourceParams.Parameters ?? new List<MySqlParameter>();
        parameters.AddParameter("Operation", MySqlDbType.UInt16, operation);
        parameters.AddParameter("Params", MySqlDbType.JSON, JsonSerializer.Serialize(json));
        return sourceParams;
    }
}
