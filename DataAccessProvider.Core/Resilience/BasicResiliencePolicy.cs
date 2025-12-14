using System.Diagnostics;
using DataAccessProvider.Core.Interfaces;

namespace DataAccessProvider.Core.Resilience;

public sealed class BasicResiliencePolicy : IResiliencePolicy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _perAttemptTimeout;

    public BasicResiliencePolicy(int maxRetries, TimeSpan perAttemptTimeout)
    {
        _maxRetries = maxRetries;
        _perAttemptTimeout = perAttemptTimeout;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        var attempt = 0;

        while (true)
        {
            attempt++;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_perAttemptTimeout);

            try
            {
                return await action(cts.Token).ConfigureAwait(false);
            }
            catch when (attempt <= _maxRetries && !cts.IsCancellationRequested)
            {
                // transient failure – retry
                continue;
            }
        }
    }
}