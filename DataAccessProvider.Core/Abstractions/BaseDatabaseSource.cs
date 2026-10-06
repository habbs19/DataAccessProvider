using System.Data;
using System.Data.Common;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core.Abstractions;

[Obsolete("Migrate to the 1.4 client, command and result API before 2.0; see docs/migration.md.", DiagnosticId = "DAP001")]

public abstract partial class BaseDatabaseSource : BaseSource, IDataSource, ICancellableDataSource
{
    protected string _connectionString { get; }
    protected IResiliencePolicy? _resiliencePolicy { get; }
    public BaseDatabaseSource(string connectionString, IResiliencePolicy? resiliencePolicy = null)
    { _connectionString = connectionString; _resiliencePolicy = resiliencePolicy; }
    public virtual DbConnection GetConnection() => throw new NotImplementedException();
    public abstract DbCommand GetCommand(string query, DbConnection connection);
    protected abstract DbParameter CreateDbParameter(DbCommand command, DataAccessParameter parameter);
    protected virtual object? ReadParameterValue(DbParameter parameter) => parameter.Value is DBNull ? null : parameter.Value;
    internal object? ReadOutputValue(DbParameter parameter) => ReadParameterValue(parameter);
    internal DbParameter BuildParameter(DbCommand command, DataAccessParameter parameter)
    {
        var native = CreateDbParameter(command, parameter);
        if (parameter.Precision.HasValue) native.Precision = parameter.Precision.Value;
        if (parameter.Scale.HasValue) native.Scale = parameter.Scale.Value;
        return native;
    }
    private sealed class LegacyProvider;
    private DatabaseClient<LegacyProvider> Client => new(new SourceProvider<LegacyProvider>(this));
    private static DatabaseCommand Definition(IBaseDatabaseSourceParams p) => new(p.Query, p.CommandType,
        p.Timeout == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(p.Timeout),
        p.Parameters?.Select(x => new DatabaseParameter(x.ParameterName, x.DbType, x.Value, x.Direction,
            x.Size == -1 ? null : x.Size, x.Precision, x.Scale)), p.RetrySafety);
    private async Task<TValue> RunLegacy<TValue>(IBaseDatabaseSourceParams p, Func<CancellationToken, Task<TValue>> action, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_resiliencePolicy is null || p.RetrySafety == RetrySafety.None) return await action(ct).ConfigureAwait(false);
        return await _resiliencePolicy.ExecuteAsync(action, ct).ConfigureAwait(false);
    }
    private static void CopyOutputs(IBaseDatabaseSourceParams request, IReadOnlyDictionary<string, object?> outputs, object? returnValue)
    {
        foreach (var parameter in request.Parameters ?? [])
        {
            if (parameter.Direction == DataAccessParameterDirection.ReturnValue) parameter.Value = returnValue;
            else if (parameter.Direction != DataAccessParameterDirection.Input && outputs.TryGetValue(parameter.ParameterName, out var value)) parameter.Value = value;
        }
    }
    protected override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams p) => ExecuteRaw(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> ExecuteRaw(BaseDataSourceParams p, CancellationToken ct)
    {
        if (p is not BaseDatabaseSourceParams request) throw new ArgumentException("Invalid database parameters.", nameof(p));
        var result = await RunLegacy(request, token => Client.QueryMultipleLegacyAsync(Definition(request), token), ct).ConfigureAwait(false);
        var sets = result.ResultSets.Select(rows => rows.Select(row => row.ToDictionary(x => x.Key, x => x.Value!)).ToList()).ToArray();
        request.SetValue(sets.Length == 1 ? sets[0].Count switch { 0 => (object)new Dictionary<string, object>(), 1 => sets[0][0], _ => sets[0] }
            : sets.Select((rows, index) => (rows, index)).ToDictionary(x => x.index, x => x.rows));
        CopyOutputs(request, result.Outputs, result.ReturnValue);
        return request;
    }
    protected override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams p)
        => ExecuteTypedFromUntyped<TValue>(p, CancellationToken.None);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams p, CancellationToken ct) where TValue : class, new()
        => ExecuteTypedFromUntyped<TValue>(p, ct);
    private async Task<BaseDataSourceParams<TValue>> ExecuteTypedFromUntyped<TValue>(BaseDataSourceParams p, CancellationToken ct) where TValue : class, new()
    {
        if (p is not BaseDatabaseSourceParams request) throw new ArgumentException("Invalid database parameters.", nameof(p));
        var result = await RunLegacy(request, token => Client.QueryLegacyAsync<TValue>(Definition(request), token), ct).ConfigureAwait(false);
        request.SetValue(result.Rows.ToList()); CopyOutputs(request, result.Outputs, result.ReturnValue);
        var typed = new TypedResult<TValue> { Query = request.Query, CommandType = request.CommandType, Timeout = request.Timeout, Parameters = request.Parameters };
        typed.SetValue(result.Rows.ToList()); return typed;
    }
    private sealed class TypedResult<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class;
    private async Task<BaseDataSourceParams<TValue>> ExecuteTyped<TValue>(BaseDataSourceParams<TValue> p, CancellationToken ct) where TValue : class, new()
    {
        if (p is not BaseDatabaseSourceParams<TValue> request) throw new ArgumentException("Invalid database parameters.", nameof(p));
        var result = await RunLegacy(request, token => Client.QueryLegacyAsync<TValue>(Definition(request), token), ct).ConfigureAwait(false);
        request.SetValue(result.Rows.ToList()); CopyOutputs(request, result.Outputs, result.ReturnValue); return request;
    }
    protected override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams p) => ExecuteWrite(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> ExecuteWrite(BaseDataSourceParams p, CancellationToken ct)
    {
        if (p is not BaseDatabaseSourceParams request) throw new ArgumentException("Invalid database parameters.", nameof(p));
        var result = await RunLegacy(request, token => Client.ExecuteAsync(Definition(request), token), ct).ConfigureAwait(false);
        request.AffectedRows = result.AffectedRows; request.SetValue(result.AffectedRows);
        CopyOutputs(request, result.Outputs, result.ReturnValue); return request;
    }
    protected override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams p) => ExecuteScalarCore(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> ExecuteScalarCore(BaseDataSourceParams p, CancellationToken ct)
    {
        if (p is not BaseDatabaseSourceParams request) throw new ArgumentException("Invalid database parameters.", nameof(p));
        var result = await RunLegacy(request, token => Client.ScalarAsync<object>(Definition(request), token), ct).ConfigureAwait(false);
        request.SetValue(result.State == ScalarState.Null ? DBNull.Value : result.Value!);
        CopyOutputs(request, result.Outputs, result.ReturnValue); return request;
    }
    protected async Task<List<Dictionary<string, object>>> ReadResultAsync(DbDataReader reader, CancellationToken cancellationToken = default)
    {
        var result = new List<Dictionary<string, object>>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = new Dictionary<string, object>();
            for (var i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
            result.Add(row);
        }
        return result;
    }
    private async Task<List<TValue>> MaterializeAsync<TValue>(DbDataReader reader, CancellationToken cancellationToken = default) where TValue : class, new()
    {
        var map = RowMapper<TValue>.Create(reader, false); var result = new List<TValue>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) result.Add(map(reader));
        return result;
    }
    public Task<bool> CheckHealthAsync() => CheckHealthAsync(CancellationToken.None);
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
        => (await Client.CheckHealthAsync(cancellationToken).ConfigureAwait(false)).Healthy;
    public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => CheckHealthAsync();
    public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => CheckHealthAsync(ct);
    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteReaderAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await ExecuteRaw(p, ct).ConfigureAwait(false);
    public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteNonQueryAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await ExecuteWrite(p, ct).ConfigureAwait(false);
    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteScalarAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await ExecuteScalarCore(p, ct).ConfigureAwait(false);
    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams p) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue> => ExecuteReaderAsync<TValue, TBaseDataSourceParams>(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue> => (TBaseDataSourceParams)await ExecuteTyped<TValue>(p, ct).ConfigureAwait(false);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> p) where TValue : class, new() => ExecuteTyped<TValue>(p, CancellationToken.None);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> p, CancellationToken ct) where TValue : class, new() => ExecuteTyped<TValue>(p, ct);
}
[Obsolete("Migrate to the 1.4 client, command and result API before 2.0; see docs/migration.md.", DiagnosticId = "DAP001")]
public abstract partial class BaseDatabaseSource<TDatabaseSourceParams> : BaseDatabaseSource, IDataSource<TDatabaseSourceParams> where TDatabaseSourceParams : BaseDatabaseSourceParams
{
    protected new IResiliencePolicy? _resiliencePolicy => base._resiliencePolicy;
    protected BaseDatabaseSource(string connectionString, IResiliencePolicy? resiliencePolicy = null) : base(connectionString, resiliencePolicy) { }
    public Task<TDatabaseSourceParams> ExecuteNonQueryAsync(TDatabaseSourceParams p) => ExecuteNonQueryAsync<TDatabaseSourceParams>(p);
    public Task<TDatabaseSourceParams> ExecuteReaderAsync(TDatabaseSourceParams p) => ExecuteReaderAsync<TDatabaseSourceParams>(p);
    public Task<TDatabaseSourceParams> ExecuteScalarAsync(TDatabaseSourceParams p) => ExecuteScalarAsync<TDatabaseSourceParams>(p);
    Task<BaseDataSourceParams<TValue>> IDataSource<TDatabaseSourceParams>.ExecuteReaderAsync<TValue>(TDatabaseSourceParams p) => ExecuteReader<TValue>(p);
}
