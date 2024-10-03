using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Interfaces;

/// <summary>
/// Abstract base class for a database implementation, providing methods for executing commands, queries, and managing connections.
/// </summary>
/// <typeparam name="TDataSourceType">The type of the data source (e.g., MSSQL, Postgres, etc.).</typeparam>
/// <typeparam name="TDbParameter">The type of database parameter (e.g., SqlParameter, NpgsqlParameter).</typeparam>
public abstract class BaseDatabase<TDataSourceType, TDbParameter> : IDatabase<TDataSourceType, TDbParameter>
    where TDataSourceType : DataSourceType
    where TDbParameter : DbParameter
{
    /// <summary>
    /// The connection string used for the database connection.
    /// </summary>
    protected string _connectionString { get; }

    /// <summary>
    /// Optional list of database parameters to be used with queries.
    /// </summary>
    protected List<DbParameter>? parameters = new List<DbParameter>();

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDatabase{TDataSourceType, TDbParameter}"/> class.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    public BaseDatabase(string connectionString)
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

    /// <summary>
    /// Executes a query and returns a reader object that allows access to the result set.
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the query.</param>
    /// <param name="commandType">The type of the SQL command (e.g., StoredProcedure, Text).</param>
    /// <param name="timeout">The command timeout in seconds.</param>
    /// <returns>A dictionary of result sets, with each result set represented as a list of key-value pairs (column names and their values).</returns>
    public virtual async Task<object> ExecuteReaderAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45)
    {
        using (var connection = GetConnection())
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                command.CommandTimeout = timeout;
                command.CommandType = commandType;
                if (parameters != null)
                    command.Parameters.AddRange(parameters.ToArray());

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
                if (resultSet.Count == 1) return resultSet[0];
                return resultSet;
            }
        }
    }

    /// <summary>
    /// Executes a non-query command, such as an INSERT, UPDATE, or DELETE operation.
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the query.</param>
    /// <param name="commandType">The type of the SQL command (e.g., StoredProcedure, Text).</param>
    /// <param name="timeout">The command timeout in seconds.</param>
    /// <returns>The number of rows affected by the query.</returns>
    public virtual async Task<int> ExecuteNonQueryAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45)
    {
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(query, connection))
            {
                command.CommandTimeout = timeout;
                command.CommandType = commandType;
                if (parameters != null)
                    command.Parameters.AddRange(parameters.ToArray());

                await connection.OpenAsync();
                return await command.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// Executes a query and maps the result to a list of objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to which each row in the result set will be mapped.</typeparam>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the query.</param>
    /// <param name="commandType">The type of the SQL command (e.g., StoredProcedure, Text).</param>
    /// <param name="timeout">The command timeout in seconds.</param>
    /// <returns>A list of objects of type <typeparamref name="T"/> populated from the result set.</returns>
    public virtual async Task<List<T>> ExecuteReaderAsync<T>(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45) where T : class, new()
    {
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(query, connection))
            {
                command.CommandTimeout = timeout;
                command.CommandType = commandType;

                if (parameters != null)
                {
                    command.Parameters.AddRange(parameters.ToArray());
                }

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    var resultSet = await ReadResultAsync(reader);
                    var result = new List<T>();

                    foreach (var row in resultSet)
                    {
                        var item = new T();
                        foreach (var property in typeof(T).GetProperties())
                        {
                            if (row.ContainsKey(property.Name) && property.CanWrite)
                            {
                                property.SetValue(item, Convert.ChangeType(row[property.Name], property.PropertyType));
                            }
                        }
                        result.Add(item);
                    }
                    return result;
                }
            }
        }
    }

    /// <summary>
    /// Executes a scalar query and returns a single value (e.g., an aggregate function result like COUNT).
    /// </summary>
    /// <param name="query">The SQL query to execute.</param>
    /// <param name="parameters">Optional parameters to pass to the query.</param>
    /// <param name="commandType">The type of the SQL command (e.g., StoredProcedure, Text).</param>
    /// <param name="timeout">The command timeout in seconds.</param>
    /// <returns>A single scalar value returned by the query.</returns>
    public Task<object> ExecuteScalarAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45)
    {
        throw new NotImplementedException();
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
