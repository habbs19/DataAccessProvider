using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.Interfaces;
using System.Text.Json;
namespace DataAccessProvider.Core.DataSource.Source;

public partial class JsonFileSource : BaseSource, IDataSource, ICancellableDataSource, IDataSource<JsonFileSourceParams>
{
    private readonly JsonFileClient _files = new();
    protected override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams p) => Read(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> Read(BaseDataSourceParams p, CancellationToken ct)
    {
        if (p is not JsonFileSourceParams request) throw new ArgumentException("Expected JsonFileSourceParams.", nameof(p));
        request.SetValue(await _files.ReadTextAsync(request.FilePath, request.Encoding, ct)); return request;
    }
    protected override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams p) => Write(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> Write(BaseDataSourceParams p, CancellationToken ct)
    {
        if (p is not JsonFileSourceParams request) throw new ArgumentException("Expected JsonFileSourceParams.", nameof(p));
        var result = await _files.WriteTextAsync(request.FilePath, request.Content, FileWriteMode.ExistingOnly, request.Encoding, ct);
        request.SetValue(checked((int)result.BytesWritten)); return request;
    }
    protected override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams p) => Scalar(p, CancellationToken.None);
    private async Task<BaseDataSourceParams> Scalar(BaseDataSourceParams p, CancellationToken ct)
    {
        if (p is not JsonFileSourceParams request) throw new ArgumentException("Expected JsonFileSourceParams.", nameof(p));
        request.SetValue(await _files.GetLengthAsync(request.FilePath, ct)); return request;
    }
    protected override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams p)
        => ExecuteReaderAsync<TValue>(p,CancellationToken.None);
    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams p,CancellationToken ct) where TValue:class,new()
    {
        if (p is not JsonFileSourceParams request) throw new ArgumentException("Expected JsonFileSourceParams.", nameof(p));
        return await ReadTyped(new JsonFileSourceParams<TValue> { FilePath = request.FilePath, Encoding = request.Encoding, SerializerOptions = request.SerializerOptions }, ct);
    }
    private async Task<BaseDataSourceParams<TValue>> ReadTyped<TValue>(BaseDataSourceParams<TValue> p, CancellationToken ct) where TValue : class, new()
    {
        if (p is not JsonFileSourceParams<TValue> request) throw new ArgumentException("Expected typed JsonFileSourceParams.", nameof(p));
        var content = await _files.ReadTextAsync(request.FilePath, request.Encoding, ct);
        var value = JsonSerializer.Deserialize<TValue>(content, request.SerializerOptions);
        request.SetValue(value is null ? [] : [value]); return request;
    }
    public Task<bool> CheckHealthAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams { ct.ThrowIfCancellationRequested(); return CheckHealthAsync(p); }
    public Task<bool> CheckHealthAsync() => Task.FromResult(false);
    public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => Task.FromResult(p is JsonFileSourceParams request && File.Exists(request.FilePath));
    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteReaderAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await Read(p, ct);
    public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteNonQueryAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await Write(p, ct);
    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams p) where TBaseDataSourceParams : BaseDataSourceParams => ExecuteScalarAsync(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TBaseDataSourceParams : BaseDataSourceParams => (TBaseDataSourceParams)await Scalar(p, ct);
    public Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams p) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue> => ExecuteReaderAsync<TValue, TBaseDataSourceParams>(p, CancellationToken.None);
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams p, CancellationToken ct) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue> => (TBaseDataSourceParams)await ReadTyped(p, ct);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> p) where TValue : class, new() => ReadTyped(p, CancellationToken.None);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> p, CancellationToken ct) where TValue : class, new() => ReadTyped(p, ct);
    public async Task<JsonFileSourceParams> ExecuteReaderAsync(JsonFileSourceParams p) => (JsonFileSourceParams)await Read(p, CancellationToken.None);
    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(JsonFileSourceParams p) where TValue : class, new() => ExecuteReader<TValue>(p);
    public async Task<JsonFileSourceParams> ExecuteNonQueryAsync(JsonFileSourceParams p) => (JsonFileSourceParams)await Write(p, CancellationToken.None);
    public async Task<JsonFileSourceParams> ExecuteScalarAsync(JsonFileSourceParams p) => (JsonFileSourceParams)await Scalar(p, CancellationToken.None);
}
