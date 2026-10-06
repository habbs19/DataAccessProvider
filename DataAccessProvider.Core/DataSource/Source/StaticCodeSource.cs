using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.Interfaces;
using System.Text;
namespace DataAccessProvider.Core.DataSource.Source;

public partial class StaticCodeSource : BaseSource, IDataSource, ICancellableDataSource, IDataSource<StaticCodeParams>
{
    public Task<BaseDataSourceParams<T>> ExecuteReaderAsync<T>(BaseDataSourceParams p, CancellationToken ct) where T:class,new()
    {ct.ThrowIfCancellationRequested();return ExecuteReader<T>(p);}
    public Task<TParams> ExecuteReaderAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams
    { ct.ThrowIfCancellationRequested(); return ExecuteReaderAsync(p); }
    public Task<TParams> ExecuteScalarAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams
    { ct.ThrowIfCancellationRequested(); return ExecuteScalarAsync(p); }
    public Task<TParams> ExecuteNonQueryAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams
    { ct.ThrowIfCancellationRequested(); return ExecuteNonQueryAsync(p); }
    public Task<TParams> ExecuteReaderAsync<T, TParams>(TParams p, CancellationToken ct) where T : class, new() where TParams : BaseDataSourceParams<T>
    { ct.ThrowIfCancellationRequested(); return ExecuteReaderAsync<T, TParams>(p); }
    public Task<BaseDataSourceParams<T>> ExecuteReaderAsync<T>(BaseDataSourceParams<T> p, CancellationToken ct) where T : class, new()
    { ct.ThrowIfCancellationRequested(); return ExecuteReaderAsync(p); }
    public Task<bool> CheckHealthAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams
    { ct.ThrowIfCancellationRequested(); return CheckHealthAsync(p); }
    protected override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams p)
    { if (p is not StaticCodeParams request) throw new ArgumentException("Expected StaticCodeParams.", nameof(p)); request.SetValue(request.Content); return Task.FromResult(p); }
    protected override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams p)
    { if (p is not StaticCodeParams request) throw new ArgumentException("Expected StaticCodeParams.", nameof(p)); request.SetValue($"{request.Content} - NonQuery executed"); return Task.FromResult(p); }
    protected override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams p)
    { if (p is not StaticCodeParams request) throw new ArgumentException("Expected StaticCodeParams.", nameof(p)); request.SetValue(Encoding.UTF8.GetByteCount(request.Content?.ToString() ?? string.Empty)); return Task.FromResult(p); }
    protected override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams p)
    { if (p is not StaticCodeParams request) throw new ArgumentException("Expected StaticCodeParams.", nameof(p)); return Typed(new StaticCodeParams<TValue> { Content = request.Content }); }
    private static Task<BaseDataSourceParams<TValue>> Typed<TValue>(BaseDataSourceParams<TValue> p) where TValue : class, new()
    { if (p is not StaticCodeParams<TValue> request) throw new ArgumentException("Expected typed StaticCodeParams.", nameof(p)); var value = (TValue?)ValueConversion.ConvertValue(request.Content, typeof(TValue), "Content"); request.SetValue(value is null ? [] : [value]); return Task.FromResult(p); }
    public Task<bool> CheckHealthAsync() => Task.FromResult(true);
    public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => CheckHealthAsync();
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await ExecuteReader(p);
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await ExecuteNonQuery(p);
    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await ExecuteScalar(p);
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams p) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue> => (TBaseDataSourceParams)await Typed(p);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> p) where TValue : class, new() => Typed(p);
    public async Task<StaticCodeParams> ExecuteReaderAsync(StaticCodeParams p) => (StaticCodeParams)await ExecuteReader(p);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(StaticCodeParams p) where TValue : class, new() => ExecuteReader<TValue>(p);
    public async Task<StaticCodeParams> ExecuteNonQueryAsync(StaticCodeParams p) => (StaticCodeParams)await ExecuteNonQuery(p);
    public async Task<StaticCodeParams> ExecuteScalarAsync(StaticCodeParams p) => (StaticCodeParams)await ExecuteScalar(p);
}
