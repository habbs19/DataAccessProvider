using System.Text;
using System.Text.Json;

namespace DataAccessProvider.Core;

public enum FileWriteMode { ExistingOnly, CreateOrOverwrite, CreateNew }
public sealed record FileWriteResult(long BytesWritten);
public interface IValueSource<T> { ValueTask<T> ReadAsync(CancellationToken cancellationToken = default); }
public sealed class StaticValueSource<T>(T value) : IValueSource<T>
{
    public ValueTask<T> ReadAsync(CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); return ValueTask.FromResult(value); }
    public async ValueTask<TResult> TransformAsync<TResult>(Func<T, TResult> transform, CancellationToken cancellationToken = default)
    { ArgumentNullException.ThrowIfNull(transform); return transform(await ReadAsync(cancellationToken).ConfigureAwait(false)); }
}
public sealed class JsonFileClient
{
    public Task<string> ReadTextAsync(string path, Encoding? encoding = null, CancellationToken cancellationToken = default)
        => File.ReadAllTextAsync(path, encoding ?? new UTF8Encoding(false), cancellationToken);
    public async Task<T?> ReadJsonAsync<T>(string path, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false);
    }
    public async Task<FileWriteResult> WriteTextAsync(string path, string content, FileWriteMode mode = FileWriteMode.ExistingOnly,
        Encoding? encoding = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); encoding ??= new UTF8Encoding(false);
        var fileMode = mode switch
        {
            FileWriteMode.ExistingOnly => FileMode.Open,
            FileWriteMode.CreateOrOverwrite => FileMode.Create,
            FileWriteMode.CreateNew => FileMode.CreateNew,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        await using var stream = new FileStream(path, fileMode, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        stream.SetLength(0);
        await using (var writer = new StreamWriter(stream, encoding, 4096, leaveOpen: true))
        {
            await writer.WriteAsync(content.AsMemory(), cancellationToken).ConfigureAwait(false);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        return new(stream.Length);
    }
    public Task<long> GetLengthAsync(string path, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(new FileInfo(path).Length); }
}
