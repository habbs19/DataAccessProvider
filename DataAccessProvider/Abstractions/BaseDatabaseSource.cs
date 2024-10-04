using System.Data;
using System.Data.Common;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using MongoDB.Driver.Core.Misc;

namespace DataAccessProvider.Abstractions;

/// <summary>
/// Represents a base class for database sources, providing common database operations like ExecuteNonQuery, ExecuteReader, and ExecuteScalar.
/// </summary>
/// <typeparam name="TDatabaseSourceParams">The type of the database source parameters.</typeparam>
public abstract class BaseDatabaseSource<TDatabaseSourceParams, TParameter> : IDataSource<TDatabaseSourceParams>, IDataSource
    where TDatabaseSourceParams : DatabaseSourceParams<TParameter>
    where TParameter : class

{
    /// <summary>
    /// The connection string used for the database connection.
    /// </summary>
    protected string _connectionString { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDatabase{TDataSourceType, TDbParameter}"/> class.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    public BaseDatabaseSource(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Gets a new instance of a database connection. Must be implemented in derived classes.
    /// </summary>
    /// <returns>A <see cref="DbConnection"/> specific to the database.</returns>
    public virtual DbConnection GetConnection()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates and returns a new command object for executing queries.
    /// Must be implemented in derived classes.
    /// </summary>
    /// <param name="query">The SQL query or command text.</param>
    /// <param name="connection">The open database connection.</param>
    /// <returns>A <see cref="DbCommand"/> object for executing commands.</returns>
    public abstract DbCommand GetCommand(string query, DbConnection connection);

    public async Task<TDatabaseSourceParams> ExecuteNonQueryAsync(TDatabaseSourceParams @params)
    {
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(@params.Query, connection))
            {
                command.CommandTimeout = @params.Timeout;
                command.CommandType = @params.CommandType;
                if (@params.Parameters != null)
                    command.Parameters.AddRange(@params.Parameters.ToArray());

                await connection.OpenAsync();
                @params.SetValue(await command.ExecuteNonQueryAsync());
                return @params;
            }
        }
    }

    /// <summary>
    /// Executes a non-query SQL command (e.g., INSERT, UPDATE, DELETE) asynchronously.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">The type of the base data source parameters.</typeparam>
    /// <param name="params">The parameters containing the query and necessary details for execution.</param>
    /// <returns>
    /// The same parameter object after execution, potentially updated with affected rows or other details.
    /// </returns>
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        var sourceParams = @params as TDatabaseSourceParams;
        return (TBaseDataSourceParams)(object)await ExecuteNonQueryAsync(sourceParams!);
    }

   
    public async Task<TDatabaseSourceParams> ExecuteReaderAsync<TValue>(TDatabaseSourceParams @params) 
        where TValue : class, new()
    {
        var sourceParams = @params as BaseDataSourceParams<TValue>;
        var result = await ExecuteReaderAsync<TValue>(sourceParams!);
        return (TDatabaseSourceParams)(object)result;
    }
    /// <summary>
    /// Executes a query asynchronously and retrieves one or more result sets from the database.
    /// Each result set is stored as a list of dictionaries, where each dictionary represents a row 
    /// and the keys represent the column names. If multiple result sets are retrieved, they are returned as a dictionary 
    /// where the keys represent the result set index.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the database parameters, which must inherit from <see cref="BaseDataSourceParams"/>. 
    /// This type contains details such as the query, command type, timeout, and any parameters required for execution.
    /// </typeparam>
    /// <param name="params">
    /// The database parameters object that includes the query to be executed, the command type (e.g., text, stored procedure), 
    /// timeout settings, and any necessary parameters for the command.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object, 
    /// with the result set(s) stored as a collection of dictionaries (where each dictionary represents a row of data).
    /// If only one result set is returned, it is stored as a single collection. If multiple result sets are returned, 
    /// they are stored in a dictionary where the keys represent the result set index (starting from 0).
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is an issue with opening the connection or executing the query.
    /// </exception>
    /// <remarks>
    /// This method handles scenarios where multiple result sets may be returned by the query (e.g., when executing 
    /// stored procedures or batch queries). Each result set is read asynchronously and stored as a list of dictionaries. 
    /// If multiple result sets are returned, they are stored in a dictionary where the key represents the index of the result set.
    /// </remarks>
    public async Task<TDatabaseSourceParams> ExecuteReaderAsync(TDatabaseSourceParams @params)
    {
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(@params.Query,connection))
            {
                command.CommandTimeout = @params.Timeout;
                command.CommandType = @params.CommandType;

                if (@params.Parameters != null)
                    command.Parameters.AddRange(@params.Parameters.ToArray());

                var resultSet = new Dictionary<int, List<Dictionary<string, object>>>();
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    int resultCount = 0;
                    do
                    {
                        resultSet[resultCount] = await ReadResultAsync(reader);
                        resultCount++;
                    }
                    while (await reader.NextResultAsync());
                }
                if (resultSet.Count == 1)
                    @params.SetValue(resultSet[0]);
                else
                    @params.SetValue(resultSet);

                return @params;
            }
        }
    }

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
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        where TValue : class, new()
    {
        var sourceParams = @params as DatabaseSourceParams<TParameter,TValue>;
        
        // Ensure the cast was successful
        if (sourceParams == null)
        {
            throw new ArgumentException("Invalid source parameters type.");
        }
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(sourceParams!.Query, connection))
            {
                command.CommandTimeout = sourceParams.Timeout;
                command.CommandType = sourceParams.CommandType;

                if (sourceParams.Parameters != null)
                {
                    command.Parameters.AddRange(sourceParams.Parameters.ToArray());
                }

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var resultSet = await ReadResultAsync(reader);
                    var result = new List<TValue>();

                    foreach (var row in resultSet)
                    {
                        var item = new TValue();
                        foreach (var property in typeof(TValue).GetProperties())
                        {
                            if (row.ContainsKey(property.Name) && property.CanWrite)
                            {
                                property.SetValue(item, Convert.ChangeType(row[property.Name], property.PropertyType));
                            }
                        }
                        result.Add(item);
                    }
                    @params.SetValue(result.AsEnumerable());
                    return @params;
                }
            }
        }
    }
    /// <summary>
    /// Executes a query asynchronously and retrieves the result set based on the provided database parameters.
    /// This method attempts to cast the provided parameters to the more specific <typeparamref name="TDatabaseSourceParams"/> type
    /// and delegates the execution to an appropriate overload of the <see cref="ExecuteReaderAsync"/> method.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the base data source parameters, which must inherit from <see cref="BaseDataSourceParams"/>.
    /// </typeparam>
    /// <param name="params">
    /// The database parameters that include the query to be executed and any necessary execution details.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object,
    /// with the result set potentially stored within it or available for further processing.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the provided <typeparamref name="TBaseDataSourceParams"/> cannot be cast to <typeparamref name="TDatabaseSourceParams"/>,
    /// which is required to execute the query.
    /// </exception>
    /// <remarks>
    /// This method attempts to cast the provided <typeparamref name="TBaseDataSourceParams"/> to <typeparamref name="TDatabaseSourceParams"/>.
    /// If the cast is successful, it delegates the execution to a more specific <see cref="ExecuteReaderAsync"/> method, which is responsible for 
    /// executing the query and processing the result set. If the cast fails, an <see cref="ArgumentException"/> is thrown to indicate an invalid 
    /// parameter type.
    /// </remarks>
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) 
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        var sourceParams = @params as TDatabaseSourceParams;

        // Ensure the cast was successful
        if (sourceParams == null)
        {
            throw new ArgumentException("Invalid source parameters type.");
        }
        return (TBaseDataSourceParams)(object)await ExecuteReaderAsync(sourceParams!);
    }

    /// <summary>
    /// Executes a query asynchronously, retrieves the result set, and maps it to a list of objects of type <typeparamref name="TValue"/>.
    /// This method invokes a more specific implementation of <see cref="ExecuteReaderAsync{TValue, TBaseDataSourceParams}"/> to handle the query execution.
    /// </summary>
    /// <typeparam name="TValue">
    /// The type to which each row in the result set will be mapped. The type <typeparamref name="TValue"/> must have a parameterless constructor 
    /// and properties corresponding to the column names in the result set.
    /// </typeparam>
    /// <param name="params">
    /// The base data source parameters that include the query to be executed, the command type, timeout, and any necessary parameters for execution.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <see cref="BaseDataSourceParams{TValue}"/> object,
    /// with the result set mapped to a list of objects of type <typeparamref name="TValue"/>.
    /// </returns>
    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params)
        where TValue : class, new()
    {
        // Call a specific ExecuteReaderAsync overload
        return await ExecuteReaderAsync<TValue, BaseDataSourceParams<TValue>>(@params);
    }

    /// <summary>
    /// Executes a scalar query asynchronously and returns the result as a single value.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the database parameters, which must inherit from <see cref="BaseDataSourceParams"/>. 
    /// This type contains details such as the query, command type, timeout, and any parameters required for execution.
    /// </typeparam>
    /// <param name="params">
    /// The database parameters that include the query to be executed and other necessary details for execution.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object, 
    /// with the scalar result value stored within it.
    /// </returns>
    public async Task<TDatabaseSourceParams> ExecuteScalarAsync(TDatabaseSourceParams @params)
    {
        return await ExecuteScalarAsync<TDatabaseSourceParams>(@params);       
    }

    /// <summary>
    /// Executes a scalar query asynchronously and returns the result as a single value.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the base data source parameters, which must inherit from <see cref="BaseDataSourceParams"/>.
    /// </typeparam>
    /// <param name="params">
    /// The base data source parameters containing the query to be executed.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object, 
    /// with the scalar result value stored within it.
    /// </returns>
    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        var sourceParams = @params as DatabaseSourceParams<TParameter>;
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(sourceParams!.Query, connection))
            {
                command.CommandTimeout = sourceParams.Timeout;
                command.CommandType = sourceParams.CommandType;

                if (sourceParams.Parameters != null)
                {
                    command.Parameters.AddRange(sourceParams.Parameters.ToArray());
                }
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                @params.SetValue(result!);
                return (TBaseDataSourceParams)(object)sourceParams!;
            }
        }
    }
  

    /// <summary>
    /// Reads the result set from a <see cref="DbDataReader"/> and maps it to a list of dictionaries.
    /// Each dictionary represents a row, with column names as keys and the corresponding values as values.
    /// </summary>
    /// <param name="reader">The <see cref="DbDataReader"/> to read from.</param>
    /// <returns>A list of dictionaries where each dictionary represents a row from the result set.</returns>
    private async Task<List<Dictionary<string, object>>> ReadResultAsync(DbDataReader reader)
    {
        var result = new List<Dictionary<string, object>>();
        var columns = reader.GetColumnSchema();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();

            foreach (var column in columns)
            {
                string columnName = column.ColumnName;
                row[columnName] = reader[columnName] is DBNull ? null : reader[columnName];
            }
            result.Add(row);
        }

        return result;
    }
}
