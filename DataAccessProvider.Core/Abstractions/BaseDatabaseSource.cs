using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core.Abstractions;

#region ExecuteMethods
public abstract partial class BaseDatabaseSource : BaseSource
{
    protected override async Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {
        var sourceParams = @params as BaseDatabaseSourceParams;
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
                    foreach (var parameter in sourceParams.Parameters)
                    {
                        command.Parameters.Add(CreateDbParameter(command, parameter));
                    }
                }

                async Task<BaseDataSourceParams> ExecuteCoreAsync(CancellationToken ct)
                {
                    var resultSet = new Dictionary<int, List<Dictionary<string, object>>>();

                    await connection.OpenAsync(ct).ConfigureAwait(false);
                    using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
                    {
                        int resultCount = 0;
                        do
                        {
                            resultSet[resultCount] = await ReadResultAsync(reader).ConfigureAwait(false);
                            resultCount++;
                        }
                        while (await reader.NextResultAsync(ct).ConfigureAwait(false));
                    }

                    if (resultSet.Count == 1)
                    {
                        var firstResultSet = resultSet[0];

                        if (firstResultSet.Count == 1)
                        {
                            sourceParams.SetValue(firstResultSet[0]);
                        }
                        else if (firstResultSet.Count == 0)
                        {
                            sourceParams.SetValue(new Dictionary<string, object>());
                        }
                        else
                        {
                            sourceParams.SetValue(firstResultSet);
                        }
                    }
                    else if (resultSet.Count > 1)
                    {
                        sourceParams.SetValue(resultSet);
                    }

                    return sourceParams;
                }

                if (_resiliencePolicy == null)
                {
                    return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
                }

                return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
            }
        }
    }

    protected override async Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params)
    {
        var sourceParams = @params as BaseDatabaseSourceParams;
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
                    foreach (var parameter in sourceParams.Parameters)
                    {
                        command.Parameters.Add(CreateDbParameter(command, parameter));
                    }
                }

                async Task<BaseDataSourceParams<TValue>> ExecuteCoreAsync(CancellationToken ct)
                {
                    await connection.OpenAsync(ct).ConfigureAwait(false);
                    using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
                    {
                        var result = await MaterializeAsync<TValue>(reader).ConfigureAwait(false);

                        if (result.Count == 1)
                        {
                            sourceParams.SetValue(result[0]);
                        }
                        else if (result.Count > 1)
                        {
                            sourceParams.SetValue(result);
                        }

                        return (BaseDataSourceParams<TValue>)(object)sourceParams;
                    }
                }

                if (_resiliencePolicy == null)
                {
                    return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
                }

                return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
            }
        }
    }

    protected async override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams @params)
    {
        var sourceParams = @params as BaseDatabaseSourceParams;
        using (var connection = GetConnection())
        {
            using (var command = GetCommand(sourceParams!.Query, connection))
            {
                command.CommandTimeout = sourceParams.Timeout;
                command.CommandType = sourceParams.CommandType;

                if (sourceParams.Parameters != null)
                {
                    foreach (var parameter in sourceParams.Parameters)
                    {
                        command.Parameters.Add(CreateDbParameter(command, parameter));
                    }
                }

                async Task<BaseDataSourceParams> ExecuteCoreAsync(CancellationToken ct)
                {
                    await connection!.OpenAsync(ct).ConfigureAwait(false);
                    var result = await command!.ExecuteScalarAsync(ct).ConfigureAwait(false);
                    sourceParams?.SetValue(result!);
                    return sourceParams!;
                }

                if (_resiliencePolicy == null)
                {
                    return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
                }

                return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
            }
        }
    }

    protected async override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams @params)
    {
        var sourceParams = @params as BaseDatabaseSourceParams;
        using (var connection = GetConnection())
        using (var command = GetCommand(sourceParams!.Query, connection))
        {
            command.CommandTimeout = sourceParams.Timeout;
            command.CommandType = sourceParams.CommandType;

            if (sourceParams.Parameters != null)
            {
                foreach (var parameter in sourceParams.Parameters)
                    {
                        command.Parameters.Add(CreateDbParameter(command, parameter));
                    }
            }

            async Task<BaseDataSourceParams> ExecuteCoreAsync(CancellationToken ct)
            {
                await connection.OpenAsync(ct).ConfigureAwait(false);
                var affectedRows = await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                sourceParams.SetValue(affectedRows);
                sourceParams.AffectedRows = affectedRows;
                return sourceParams;
            }

            if (_resiliencePolicy == null)
            {
                return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
            }

            return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
        }
    }
}

#endregion ExecuteMethods
#region Props
/// <summary>
/// Represents a base class for database sources, providing common database operations like ExecuteNonQuery, ExecuteReader, and ExecuteScalar.
/// </summary>
/// <typeparam name="TDatabaseSourceParams">The type of the database source parameters.</typeparam>
public abstract partial class BaseDatabaseSource
{
    /// <summary>
    /// The connection string used for the database connection.
    /// </summary>
    protected string _connectionString { get; }
    protected IResiliencePolicy? _resiliencePolicy { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseDatabase{TDataSourceType, TDbParameter}"/> class.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    public BaseDatabaseSource(string connectionString, IResiliencePolicy? resiliencePolicy = null)
    {
        _connectionString = connectionString;
        _resiliencePolicy = resiliencePolicy;
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

    protected abstract DbParameter CreateDbParameter(DbCommand command, DataAccessParameter parameter);


    /// <summary>
    /// Reads the result set from a <see cref="DbDataReader"/> and maps it to a list of dictionaries.
    /// Each dictionary represents a row, with column names as keys and the corresponding values as values.
    /// </summary>
    /// <param name="reader">The <see cref="DbDataReader"/> to read from.</param>
    /// <returns>A list of dictionaries where each dictionary represents a row from the result set.</returns>
    protected async Task<List<Dictionary<string, object>>> ReadResultAsync(DbDataReader reader)
    {
        var schema = reader.GetColumnSchema();
        var columns = schema
            .Where(column => column.ColumnOrdinal.HasValue && !string.IsNullOrEmpty(column.ColumnName))
            .Select(column => (Name: column.ColumnName!, Ordinal: column.ColumnOrdinal!.Value))
            .ToArray();

        var result = new List<Dictionary<string, object>>();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>(columns.Length);

            foreach (var (name, ordinal) in columns)
            {
                object? value = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
                row[name] = value!;
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
public abstract partial class BaseDatabaseSource : IDataSource
{
    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            using var connection = GetConnection();
            await connection.OpenAsync().ConfigureAwait(false);
            return connection.State == System.Data.ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        return CheckHealthAsync();
    }

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
        var sourceParams = @params as BaseDatabaseSourceParams;
        return (TBaseDataSourceParams)await ExecuteReader<TValue>(sourceParams!);
    }

    private object? GetPropertyValue<TValue>(PropertyInfo property, object? value) where TValue : class, new()
    {
        var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (value == null || value == DBNull.Value)
            return null;

        if (targetType.IsInstanceOfType(value))
            return value;

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
        var sourceParams = @params as BaseDatabaseSourceParams<TValue>;
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
                    foreach (var parameter in sourceParams.Parameters)
                    {
                        command.Parameters.Add(CreateDbParameter(command, parameter));
                    }
                }

                async Task<BaseDataSourceParams<TValue>> ExecuteCoreAsync(CancellationToken ct)
                {
                    await connection.OpenAsync(ct).ConfigureAwait(false);
                    using (var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false))
                    {
                        var result = await MaterializeAsync<TValue>(reader).ConfigureAwait(false);

                        if (result.Count == 1)
                        {
                            sourceParams.SetValue(result[0]);
                        }
                        else if (result.Count > 1)
                        {
                            sourceParams.SetValue(result);
                        }

                        return (BaseDataSourceParams<TValue>)(object)sourceParams;
                    }
                }

                if (_resiliencePolicy == null)
                {
                    return await ExecuteCoreAsync(CancellationToken.None).ConfigureAwait(false);
                }

                return await _resiliencePolicy.ExecuteAsync(ExecuteCoreAsync).ConfigureAwait(false);
            }
        }
    }

    private async Task<List<TValue>> MaterializeAsync<TValue>(DbDataReader reader) where TValue : class, new()
    {
        var accessors = TypeAccessorCache<TValue>.GetColumnAccessors(reader);
        var result = new List<TValue>();

        if (accessors.Length == 0)
        {
            while (await reader.ReadAsync())
            {
                result.Add(new TValue());
            }

            return result;
        }

        while (await reader.ReadAsync())
        {
            var item = new TValue();

            foreach (var accessor in accessors)
            {
                object? rawValue = reader.IsDBNull(accessor.Ordinal) ? null : reader.GetValue(accessor.Ordinal);

                if (rawValue == null)
                {
                    if (accessor.AllowNullAssignments)
                    {
                        accessor.Assign(item, null);
                    }

                    continue;
                }

                var converted = GetPropertyValue<TValue>(accessor.Property, rawValue);

                if (converted == null)
                {
                    if (accessor.AllowNullAssignments)
                    {
                        accessor.Assign(item, null);
                    }

                    continue;
                }

                accessor.Assign(item, converted);
            }

            result.Add(item);
        }

        return result;
    }

    private static class TypeAccessorCache<T>
        where T : class, new()
    {
        private static readonly PropertyAccessor[] _writableProperties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite)
            .Select(p => new PropertyAccessor(p))
            .ToArray();

        private static readonly Dictionary<string, PropertyAccessor> _lookup = _writableProperties
            .GroupBy(p => p.Property.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        public static PropertyColumnAccessor<T>[] GetColumnAccessors(DbDataReader reader)
        {
            var matched = new List<PropertyColumnAccessor<T>>(_writableProperties.Length);

            for (var ordinal = 0; ordinal < reader.FieldCount; ordinal++)
            {
                var columnName = reader.GetName(ordinal);
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    continue;
                }

                if (_lookup.TryGetValue(columnName, out var accessor))
                {
                    matched.Add(new PropertyColumnAccessor<T>(accessor.Property, accessor.Setter, ordinal));
                }
            }

            return matched.ToArray();
        }

        private sealed class PropertyAccessor
        {
            public PropertyAccessor(PropertyInfo property)
            {
                Property = property;
                Setter = CreateSetter(property);
            }

            public PropertyInfo Property { get; }

            public Action<T, object?> Setter { get; }

            private static Action<T, object?> CreateSetter(PropertyInfo property)
            {
                var target = Expression.Parameter(typeof(T), "target");
                var value = Expression.Parameter(typeof(object), "value");

                var convertedValue = Expression.Condition(
                    Expression.Equal(value, Expression.Constant(null)),
                    Expression.Default(property.PropertyType),
                    Expression.Convert(value, property.PropertyType));

                var body = Expression.Assign(Expression.Property(target, property), convertedValue);

                return Expression.Lambda<Action<T, object?>>(body, target, value).Compile();
            }
        }
    }

    private readonly struct PropertyColumnAccessor<T>
        where T : class
    {
        private readonly Action<T, object?> _setter;

        public PropertyColumnAccessor(PropertyInfo property, Action<T, object?> setter, int ordinal)
        {
            Property = property;
            _setter = setter;
            Ordinal = ordinal;
            AllowNullAssignments = !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        public PropertyInfo Property { get; }

        public int Ordinal { get; }

        public bool AllowNullAssignments { get; }

        public void Assign(T target, object? value) => _setter(target, value);
    }
}
#endregion BaseDatabaseSource
#region BaseDatabaseSource<TDatabaseSourceParams>
/// <summary>
/// Represents a base class for database sources, providing common database operations like ExecuteNonQuery, ExecuteReader, and ExecuteScalar.
/// </summary>
/// <typeparam name="TDatabaseSourceParams">The type of the database source parameters.</typeparam>
public abstract partial class BaseDatabaseSource<TDatabaseSourceParams> : BaseDatabaseSource, IDataSource<TDatabaseSourceParams>
    where TDatabaseSourceParams : BaseDatabaseSourceParams
{
    protected IResiliencePolicy? _resiliencePolicy { get; }

    protected BaseDatabaseSource(string connectionString, IResiliencePolicy? resiliencePolicy = null) : base(connectionString)
    {
        _resiliencePolicy = resiliencePolicy;
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
#endregion BaseDatabaseSource<TDatabaseSourceParams>  
