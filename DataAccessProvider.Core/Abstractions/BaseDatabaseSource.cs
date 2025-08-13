using System.Data.Common;
using System.Text.Json;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;

namespace DataAccessProvider.Core.Abstractions;

#region ExecuteMethods
public abstract partial class BaseDatabaseSource<TParameter> : BaseSource
{
    protected override async Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
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
                    command.Parameters.AddRange(sourceParams.Parameters.ToArray());

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
                // Handle single result set or multiple result sets
                if (resultSet.Count == 1)
                {
                    var firstResultSet = resultSet[0];

                    // Handle single row, empty result, or list
                    if (firstResultSet.Count == 1)
                    {
                        // Return the single row
                        sourceParams.SetValue(firstResultSet[0]);
                    }
                    else if (firstResultSet.Count == 0)
                    {
                        // Return an empty dictionary if no results
                        sourceParams.SetValue(new Dictionary<string, object>());
                    }
                    else
                    {
                        // Return the entire list for multiple rows
                        sourceParams.SetValue(firstResultSet);
                    }
                }
                else if (resultSet.Count > 1)
                {
                    // Handle multiple result sets
                    sourceParams.SetValue(resultSet);
                }
                return sourceParams;
            }
        }
    }

    protected override async Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params) where TValue : class
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
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
                    var result = new List<TValue>();
                    var columns = reader.GetColumnSchema(); 

                    while (await reader.ReadAsync())
                    {
                        var item = new TValue();  

                        foreach (var property in typeof(TValue).GetProperties())
                        {
                            var column = columns.FirstOrDefault(c => c.ColumnName.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
                            if (column != null && property.CanWrite)
                            {
                                var value = reader[property.Name];
                                property.SetPropertyValue(item,value);                              
                            }
                        }
                        result.Add(item);
                    }

                    if (result.Count == 1)
                    {
                        sourceParams.SetValue(result[0]);
                    }
                    else if(result.Count > 1)
                    {
                        sourceParams.SetValue(result);
                    }

                    //var converted = Convert.ChangeType(sourceParams, typeof(BaseDataSourceParams<TValue>));
                    return (BaseDataSourceParams<TValue>)(object)sourceParams;
                }
            }
        }
    }
    protected async override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams @params)
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
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
                sourceParams.SetValue(result!);
                return (BaseDataSourceParams)(object)sourceParams!;
            }
        }
    }

    protected async override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams @params)
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(sourceParams!.Query, connection))
            {
                command.CommandTimeout = sourceParams.Timeout;
                command.CommandType = sourceParams.CommandType;
                if (sourceParams.Parameters != null)
                    command.Parameters.AddRange(sourceParams.Parameters.ToArray());

                await connection.OpenAsync();
                var affectedRows = await command.ExecuteNonQueryAsync();
                sourceParams.SetValue(affectedRows);
                sourceParams.AffectedRows = affectedRows;
                return sourceParams;
            }
        }
    }
}
#endregion ExecuteMethods
#region Props
/// <summary>
/// Represents a base class for database sources, providing common database operations like ExecuteNonQuery, ExecuteReader, and ExecuteScalar.
/// </summary>
/// <typeparam name="TDatabaseSourceParams">The type of the database source parameters.</typeparam>
public abstract partial class BaseDatabaseSource<TParameter>
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

    /// <summary>
    /// Reads the result set from a <see cref="DbDataReader"/> and maps it to a list of dictionaries.
    /// Each dictionary represents a row, with column names as keys and the corresponding values as values.
    /// </summary>
    /// <param name="reader">The <see cref="DbDataReader"/> to read from.</param>
    /// <returns>A list of dictionaries where each dictionary represents a row from the result set.</returns>
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
}

#endregion 
#region BaseDatabaseSource
/// <summary>
/// Represents a base class for database sources, providing common database operations like ExecuteNonQuery, ExecuteReader, and ExecuteScalar.
/// </summary>
/// <typeparam name="TDatabaseSourceParams">The type of the database source parameters.</typeparam>
public abstract partial class BaseDatabaseSource<TParameter> : IDataSource
    where TParameter : DbParameter
{
    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
       where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteScalar(@params);   
    }
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
       where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)(object)await ExecuteReader(@params);
    }

    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)(object)await ExecuteNonQuery(@params);
    }
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        where TValue : class, new()
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
        return (TBaseDataSourceParams)await ExecuteReader<TValue>(sourceParams!);
    }

    private object? GetPropertyValue<TValue>(System.Reflection.PropertyInfo property, object? value) where TValue : class, new()
    {
        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            return targetType switch
            {
                Type t when t.IsEnum => value switch
                {
                    null => default, // or throw if enums are required

                    // Direct string input
                    string s => Enum.Parse(t, s, ignoreCase: true),

                    // JSON string value
                    JsonElement je when je.ValueKind == JsonValueKind.String
                        => Enum.Parse(t, je.GetString() ?? string.Empty, ignoreCase: true),

                    // JSON number value
                    JsonElement je when je.ValueKind == JsonValueKind.Number
                        => Enum.ToObject(t, je.GetInt32()),

                    // Already the correct underlying type (int, byte, etc.)
                    int i => Enum.ToObject(t, i),
                    long l => Enum.ToObject(t, l),

                    // Fallback - try to convert to string and parse
                    _ => Enum.Parse(t, value.ToString() ?? string.Empty, ignoreCase: true)
                },

                Type t when t == typeof(bool) =>
                    value is string strVal ? (strVal == "1" || strVal.Equals("true", StringComparison.OrdinalIgnoreCase)) : Convert.ToBoolean(value),

                Type t when t == typeof(DateTime) =>
                    Convert.ToDateTime(value),

                Type t when t == typeof(Guid) =>
                    value switch
                    {
                        string g => Guid.Parse(g),
                        byte[] b => new Guid(b),
                        _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to Guid")
                    },

                Type t when t == typeof(TimeSpan) =>
                    value switch
                    {
                        string tsStr => TimeSpan.Parse(tsStr),
                        long ticks => TimeSpan.FromTicks(ticks),
                        _ => throw new InvalidCastException($"Cannot convert {value.GetType()} to TimeSpan")
                    },

                Type t when t == typeof(byte[]) =>
                    (byte[])value,

                Type t when t.IsPrimitive || t == typeof(decimal) =>
                    Convert.ChangeType(value, t),

                Type t when t == typeof(string) =>
                    value.ToString(),

                _ => Convert.ChangeType(value, targetType),
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error setting property '{property.Name}' on type '{typeof(TValue).Name}' with value '{value ?? "null"}' ({value?.GetType().Name ?? "null"}).",
                ex
            );
        }
    }



    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter,TValue>;
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
                            if (!row.ContainsKey(property.Name) || !property.CanWrite)
                                continue;

                            var value = row[property.Name];
                            if (value == DBNull.Value)
                                value = null;

                            // Handle Enum, Boolean, DateTime, and other conversions
                            value = GetPropertyValue<TValue>(property, value);

                            if (value != null)
                            {
                                property.SetPropertyValue(item, value);
                            }

                        }
                        result.Add(item);
                    }

                    if (result.Count == 1)
                    {
                        sourceParams.SetValue(result[0]);
                    }
                    else if (result.Count > 1)
                    {
                        sourceParams.SetValue(result);
                    }

                    //var converted = Convert.ChangeType(sourceParams, typeof(BaseDataSourceParams<TValue>));
                    return (BaseDataSourceParams<TValue>)(object)sourceParams;
                }
            }
        }


    }
}
#endregion BaseDatabaseSource
#region BaseDatabaseSource<>
/// <summary>
/// Represents a base class for database sources, providing common database operations like ExecuteNonQuery, ExecuteReader, and ExecuteScalar.
/// </summary>
/// <typeparam name="TDatabaseSourceParams">The type of the database source parameters.</typeparam>
public abstract partial class BaseDatabaseSource<TParameter, TDatabaseSourceParams> : BaseDatabaseSource<TParameter>, IDataSource<TDatabaseSourceParams>
    where TDatabaseSourceParams : BaseDatabaseSourceParams<TParameter>
    where TParameter : DbParameter
{
    protected BaseDatabaseSource(string connectionString) : base(connectionString)
    {
    }

    public async Task<TDatabaseSourceParams> ExecuteNonQueryAsync(TDatabaseSourceParams @params)
    {
        return (TDatabaseSourceParams)(object)await ExecuteNonQuery(@params);
    }
    public async Task<TDatabaseSourceParams> ExecuteReaderAsync(TDatabaseSourceParams @params)
    {
        return (TDatabaseSourceParams)(object)await ExecuteReader(@params);
    }
    public async Task<TDatabaseSourceParams> ExecuteScalarAsync(TDatabaseSourceParams @params)
    {
        return (TDatabaseSourceParams)(object)await ExecuteScalar(@params);       
    }
    async Task<BaseDataSourceParams<TValue>> IDataSource<TDatabaseSourceParams>.ExecuteReaderAsync<TValue>(TDatabaseSourceParams @params)
    {
        return await ExecuteReader<TValue>(@params!);
    }
}
#endregion BaseDatabaseSource<>  