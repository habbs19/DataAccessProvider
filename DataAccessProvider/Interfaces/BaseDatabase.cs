using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Interfaces;
public abstract class BaseDatabase<TDatabaseType> : IDatabase<TDatabaseType> where TDatabaseType : DatabaseType
{
    protected string _connectionString { get; }

    public BaseDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected abstract DbConnection GetConnection();
    protected abstract DbCommand GetCommand(string query, DbConnection connection);

    protected async Task<List<Dictionary<string, object>>> ReadResultAsync(DbDataReader reader)
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

    public async Task<object> ExecuteReaderAsync(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure)
    {
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(query, connection))
            {
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

    public async Task<int> ExecuteNonQueryAsync<TConnection, TCommand>(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure)
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
}