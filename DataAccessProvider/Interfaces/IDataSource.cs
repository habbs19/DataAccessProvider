using DataAccessProvider.Abstractions;
namespace DataAccessProvider.Interfaces;

#region IDataSource
public interface IDataSource
{
    /// <summary>
    /// Executes a query asynchronously, retrieves the result set, and maps it to a list of objects of type <typeparamref name="TValue"/>.
    /// This method uses the provided database parameters to execute the query and map the results into a collection of objects.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type to which each row in the result set will be mapped. The type <typeparamref name="TValue"/> must have a parameterless constructor 
    /// and properties corresponding to the column names in the result set.
    /// </typeparam>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the base data source parameters, which must inherit from <see cref="BaseDataSourceParams{TValue}"/>.
    /// This type contains details such as the query, command type, timeout, and any parameters required for execution.
    /// </typeparam>
    /// <param name="params">
    /// The database parameters that include the query to be executed, the command type (e.g., text, stored procedure), 
    /// timeout settings, and any necessary parameters to be added to the command.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object, 
    /// with the result set mapped to a list of objects of type <typeparamref name="TValue"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is an error opening the connection or executing the query.
    /// </exception>
    /// <remarks>
    /// This method automatically maps each row from the result set to an object of type <typeparamref name="TValue"/> by matching the column names
    /// with the property names of <typeparamref name="TValue"/>. Ensure that <typeparamref name="TValue"/> has a parameterless constructor and writable properties.
    /// </remarks>
    Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue,TBaseDataSourceParams>(TBaseDataSourceParams @params) 
        where TBaseDataSourceParams : BaseDataSourceParams<TValue> 
        where TValue : class, new();

    Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params)
        where TValue : class, new();

    /// <summary>
    /// Executes a query asynchronously and retrieves a result set based on the provided data source parameters.
    /// This method is typically used to read data (e.g., SELECT queries) and return the result to the provided parameters.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the base data source parameters, which must inherit from <see cref="BaseDataSourceParams"/>. 
    /// These parameters contain the query and other execution-related information.
    /// </typeparam>
    /// <param name="params">
    /// The data source parameters that include the query and any additional details required for execution.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object, 
    /// potentially updated with the results of the query execution.
    /// </returns>
    /// <remarks>
    /// This method is designed for read operations (e.g., SELECT queries) where data needs to be retrieved from the data source. 
    /// The result is typically processed and stored in the provided parameters.
    /// </remarks>
    Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) 
        where TBaseDataSourceParams : BaseDataSourceParams;

  

    /// <summary>
    /// Executes a non-query command asynchronously, such as INSERT, UPDATE, or DELETE, based on the provided data source parameters.
    /// This method is used to execute commands that do not return result sets.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the base data source parameters, which must inherit from <see cref="BaseDataSourceParams"/>. 
    /// These parameters contain the query and other execution-related information.
    /// </typeparam>
    /// <param name="params">
    /// The data source parameters that include the command and any additional details required for execution.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object, 
    /// potentially updated with details such as the number of affected rows or other relevant information.
    /// </returns>
    /// <remarks>
    /// This method is intended for non-query operations such as INSERT, UPDATE, DELETE, or any command that modifies the data 
    /// without returning a result set.
    /// </remarks>
    Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) 
        where TBaseDataSourceParams : BaseDataSourceParams;

    /// <summary>
    /// Executes a scalar command asynchronously and returns a single value, typically used for aggregate queries (e.g., COUNT, SUM).
    /// This method is used when a single value is expected as the result of the query.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the base data source parameters, which must inherit from <see cref="BaseDataSourceParams"/>. 
    /// These parameters contain the query and other execution-related information.
    /// </typeparam>
    /// <param name="params">
    /// The data source parameters that include the query and any additional details required for execution.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object, 
    /// with the scalar result stored within it.
    /// </returns>
    /// <remarks>
    /// This method is typically used for aggregate functions or queries where a single value (such as COUNT, SUM, or an ID) is expected.
    /// </remarks>
    Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) 
        where TBaseDataSourceParams : BaseDataSourceParams;
}
#endregion IDataSource

#region IDataSource<>
public interface IDataSource<TBaseDataSourceParams> 
    where TBaseDataSourceParams : BaseDataSourceParams
{
    /// <summary>
    /// Executes a query asynchronously and retrieves a result set, mapping the result to a list of objects of type <typeparamref name="TValue"/>.
    /// This method is used when the result needs to be mapped to a specific model or entity type.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type of the model or entity that the result set will be mapped to. The type must have a parameterless constructor.
    /// </typeparam>
    /// <param name="params">
    /// The base data source parameters containing the query and any relevant execution details, such as parameters and timeouts.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object,
    /// with the results mapped to objects of type <typeparamref name="TValue"/>.
    /// </returns>
    /// <remarks>
    /// This method is typically used for reading data and mapping it to a strongly-typed collection of <typeparamref name="TValue"/> objects.
    /// </remarks>
    Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(TBaseDataSourceParams @params)
        where TValue : class, new();

    /// <summary>
    /// Executes a query asynchronously and retrieves a result set based on the provided data source parameters.
    /// This method is typically used for read operations where the result set does not need to be mapped to specific types.
    /// </summary>
    /// <param name="params">
    /// The base data source parameters that contain the query and any relevant details for execution, such as parameters and timeouts.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object,
    /// potentially updated with the results of the query execution.
    /// </returns>
    /// <remarks>
    /// This method is useful for read operations where the data does not need to be mapped to specific objects or models.
    /// </remarks>
    Task<TBaseDataSourceParams> ExecuteReaderAsync(TBaseDataSourceParams @params);

    /// <summary>
    /// Executes a non-query command asynchronously, such as an INSERT, UPDATE, or DELETE statement.
    /// This method is used to perform data modification operations that do not return a result set.
    /// </summary>
    /// <param name="params">
    /// The base data source parameters that contain the command (e.g., INSERT, UPDATE, DELETE) and any relevant execution details, 
    /// such as parameters and timeouts.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object,
    /// potentially updated with details such as the number of affected rows.
    /// </returns>
    /// <remarks>
    /// This method is used for commands that modify data in the data source without returning a result set.
    /// </remarks>
    Task<TBaseDataSourceParams> ExecuteNonQueryAsync(TBaseDataSourceParams @params);

    /// <summary>
    /// Executes a scalar command asynchronously and returns a single value, typically used for aggregate queries such as COUNT or SUM.
    /// This method is used when the query is expected to return a single value.
    /// </summary>
    /// <param name="params">
    /// The base data source parameters that contain the query and any relevant execution details, such as parameters and timeouts.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object,
    /// with the scalar result stored within it.
    /// </returns>
    /// <remarks>
    /// This method is typically used for queries that return a single value, such as aggregate functions (COUNT, SUM) or scalar values like IDs.
    /// </remarks>
    Task<TBaseDataSourceParams> ExecuteScalarAsync(TBaseDataSourceParams @params);
}
#endregion IDataSource<>