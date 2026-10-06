using System.Data;
using System.Data.Common;
using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core;

public enum RetrySafety { None, ReadOnly, Idempotent }
public enum ResourceOwnership { Borrowed, Owned }
public enum ScalarState { NoRow, Null, Value }
public sealed record DatabaseCapabilities(bool StoredProcedures = true, bool OutputParameters = true, bool Transactions = true)
{
    public IReadOnlyCollection<IsolationLevel>? IsolationLevels { get; init; }
}

/// <summary>Immutable parameter definition. Mutable byte values are copied at construction and access.</summary>
public sealed class DatabaseParameter
{
    private readonly object? _value;
    public string Name { get; }
    public DataAccessDbType Type { get; }
    public object? Value => _value is byte[] bytes ? bytes.ToArray() : _value;
    public DataAccessParameterDirection Direction { get; }
    public int? Size { get; }
    public byte? Precision { get; }
    public byte? Scale { get; }
    public DatabaseParameter(string name, DataAccessDbType type, object? value = null,
        DataAccessParameterDirection direction = DataAccessParameterDirection.Input,
        int? size = null, byte? precision = null, byte? scale = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        Name = name; Type = type; _value = value is byte[] bytes ? bytes.ToArray() : value;
        Direction = direction; Size = size; Precision = precision; Scale = scale;
    }
}

public sealed class DatabaseCommand
{
    public string CommandText { get; }
    public CommandType CommandType { get; }
    public TimeSpan Timeout { get; }
    public RetrySafety RetrySafety { get; }
    public IReadOnlyList<DatabaseParameter> Parameters { get; }
    public DatabaseCommand(string commandText, CommandType commandType = CommandType.Text,
        TimeSpan? timeout = null, IEnumerable<DatabaseParameter>? parameters = null,
        RetrySafety retrySafety = RetrySafety.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);
        if (commandType is not (CommandType.Text or CommandType.StoredProcedure)) throw new ArgumentOutOfRangeException(nameof(commandType));
        if (!Enum.IsDefined(retrySafety)) throw new ArgumentOutOfRangeException(nameof(retrySafety));
        CommandText = commandText; CommandType = commandType;
        Timeout = timeout ?? TimeSpan.FromSeconds(30);
        if (Timeout != System.Threading.Timeout.InfiniteTimeSpan && (Timeout <= TimeSpan.Zero || Timeout.TotalMilliseconds > uint.MaxValue - 1))
            throw new ArgumentOutOfRangeException(nameof(timeout));
        RetrySafety = retrySafety;
        Parameters = Array.AsReadOnly((parameters ?? []).ToArray());
        if (Parameters.Any(p => p is null)) throw new ArgumentException("Parameters cannot contain null entries.", nameof(parameters));
        if (Parameters.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != Parameters.Count)
            throw new ArgumentException("Parameter names must be unique.", nameof(parameters));
    }
    public static DatabaseCommand Text(string text) => new(text);
    public static DatabaseCommand StoredProcedure(string name) => new(name, CommandType.StoredProcedure);
    public DatabaseCommand WithParameter(string name, DataAccessDbType type, object? value,
        int? size = null, byte? precision = null, byte? scale = null) => Append(new(name, type, value, size: size, precision: precision, scale: scale));
    public DatabaseCommand WithOutput(string name, DataAccessDbType type, int? size = null) => Append(new(name, type, direction: DataAccessParameterDirection.Output, size: size));
    public DatabaseCommand WithReturnValue(string name, DataAccessDbType type) => Append(new(name, type, direction: DataAccessParameterDirection.ReturnValue));
    public DatabaseCommand WithTimeout(TimeSpan timeout) => new(CommandText, CommandType, timeout, Parameters, RetrySafety);
    public DatabaseCommand WithRetrySafety(RetrySafety safety) => new(CommandText, CommandType, Timeout, Parameters, safety);
    private DatabaseCommand Append(DatabaseParameter parameter) => new(CommandText, CommandType, Timeout, Parameters.Append(parameter), RetrySafety);
}

public sealed record QueryResult<T>(IReadOnlyList<T> Rows, IReadOnlyDictionary<string, object?> Outputs, object? ReturnValue);
public sealed record ScalarResult<T>(ScalarState State, T? Value, IReadOnlyDictionary<string, object?> Outputs, object? ReturnValue);
public sealed record CommandResult(int AffectedRows, IReadOnlyDictionary<string, object?> Outputs, object? ReturnValue);
public sealed record MultipleResult(IReadOnlyList<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ResultSets,
    IReadOnlyDictionary<string, object?> Outputs, object? ReturnValue);
public enum HealthFailure { None, Configuration, Authentication, Connectivity, Unknown }
public sealed record HealthResult(bool Healthy, HealthFailure Failure, string? ProviderCode, TimeSpan Duration);

public interface IDatabaseSession
{
    Task<QueryResult<T>> QueryAsync<T>(DatabaseCommand command, CancellationToken cancellationToken = default) where T : class, new();
    Task<QueryResult<T>> QueryAsync<T>(DatabaseCommand command, Func<DbDataReader, T> mapper, CancellationToken cancellationToken = default);
    Task<ScalarResult<T>> ScalarAsync<T>(DatabaseCommand command, CancellationToken cancellationToken = default);
    Task<CommandResult> ExecuteAsync(DatabaseCommand command, CancellationToken cancellationToken = default);
    Task<MultipleResult> QueryMultipleAsync(DatabaseCommand command, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteNativeAsync<TResult>(Func<DbConnection, DbTransaction?, CancellationToken, Task<TResult>> action, CancellationToken cancellationToken = default);
}
public interface IDatabaseClient<TProvider> : IDatabaseSession, IAsyncDisposable
{
    IAsyncEnumerable<T> StreamAsync<T>(DatabaseCommand command, CancellationToken cancellationToken = default) where T : class, new();
    IAsyncEnumerable<T> StreamAsync<T>(DatabaseCommand command, Func<DbDataReader, T> mapper, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<IDatabaseSession, CancellationToken, Task<TResult>> action,
        IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<IDatabaseSession, CancellationToken, Task> action,
        IsolationLevel? isolationLevel = null, CancellationToken cancellationToken = default);
    Task<HealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
/// <summary>Custom provider boundary. Connections returned here must be new, caller-owned logical connections.</summary>
public interface IRelationalProvider<TProvider> : IAsyncDisposable
{
    DatabaseCapabilities Capabilities { get; }
    DbConnection CreateConnection();
    DbCommand CreateCommand(DbConnection connection, DatabaseCommand definition);
    DbParameter CreateParameter(DbCommand command, DatabaseParameter definition);
    object? ReadOutput(DbParameter parameter) => parameter.Value is DBNull ? null : parameter.Value;
    bool IsTransient(Exception exception) => exception is DbException { IsTransient: true };
    HealthFailure ClassifyHealthFailure(Exception exception) => exception is ArgumentException ? HealthFailure.Configuration : HealthFailure.Unknown;
    void Validate(DatabaseCommand command) { }
    ValueTask IAsyncDisposable.DisposeAsync() => ValueTask.CompletedTask;
}
public sealed class DatabaseClientOptions
{
    public int MaxRetries { get; set; }
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(200);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(5);
    public Func<Exception, bool>? IsTransient { get; set; }
    public Func<TimeSpan, CancellationToken, Task> DelayAsync { get; set; } = Task.Delay;
}
