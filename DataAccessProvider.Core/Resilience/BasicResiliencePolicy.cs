using System.Data.Common;
using DataAccessProvider.Core.Interfaces;
namespace DataAccessProvider.Core.Resilience;

public sealed class BasicResiliencePolicy : IResiliencePolicy
{
    private readonly int _maxRetries; private readonly TimeSpan _timeout;
    private readonly Func<Exception, bool> _transient;
    public BasicResiliencePolicy(int maxRetries, TimeSpan perAttemptTimeout) : this(maxRetries, perAttemptTimeout, ex => ex is DbException { IsTransient: true }) { }
    public BasicResiliencePolicy(int maxRetries, TimeSpan perAttemptTimeout, Func<Exception, bool> isTransient)
    {
        if (maxRetries < 0) throw new ArgumentOutOfRangeException(nameof(maxRetries));
        if (perAttemptTimeout != Timeout.InfiniteTimeSpan && (perAttemptTimeout <= TimeSpan.Zero || perAttemptTimeout.TotalMilliseconds > uint.MaxValue - 1)) throw new ArgumentOutOfRangeException(nameof(perAttemptTimeout));
        _maxRetries = maxRetries; _timeout = perAttemptTimeout; _transient = isTransient ?? throw new ArgumentNullException(nameof(isTransient));
    }
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_timeout != Timeout.InfiniteTimeSpan) timeout.CancelAfter(_timeout);
            try { return await action(timeout.Token).ConfigureAwait(false); }
            catch (Exception ex) when (attempt < _maxRetries && !timeout.IsCancellationRequested &&
                ex is not (OperationCanceledException or ArgumentException or MappingException or NotSupportedException or FormatException) && _transient(ex))
            { await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(5000, 200 * Math.Pow(2, attempt)) * (0.8 + Random.Shared.NextDouble() * 0.2)), cancellationToken).ConfigureAwait(false); }
        }
    }
}
