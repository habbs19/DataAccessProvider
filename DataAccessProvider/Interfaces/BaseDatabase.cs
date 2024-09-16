using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Interfaces;
public abstract class BaseDatabase<TDatabaseType, TDbParameter> : IDatabase<TDatabaseType, TDbParameter> where TDatabaseType : DatabaseType where TDbParameter : DbParameter
{
    protected string _connectionString { get; }
    protected List<DbParameter>? parameters = new List<DbParameter> { };

    public BaseDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }
    public virtual DbConnection GetConnection()
    {
        throw new NotImplementedException();
    }
    public abstract DbCommand GetCommand(string query, DbConnection connection);

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
                return resultSet;
            }
        }
    }

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

    public virtual async Task<List<T>> ExecuteQueryAsync<T>(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45) where T : class, new()
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

    public Task<object> ExecuteScalarAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45)
    {
        throw new NotImplementedException();
    }

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