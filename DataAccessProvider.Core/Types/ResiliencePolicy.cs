using System.Data.Common;
using Polly;
using Polly.Timeout;

namespace DataAccessProvider.Core.Types;

public sealed class ResiliencePolicy
{
    private readonly AsyncPolicyWrap _policy;

    private ResiliencePolicy(AsyncPolicyWrap policy)
    {
        _policy = policy;
    }

    public static ResiliencePolicy Create(DatabaseResilienceOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var timeoutPolicy = Policy.TimeoutAsync(options.OperationTimeout);

        var retryPolicy = Policy
            .Handle<DbException>()
            .Or<TimeoutException>()
            .Or<TimeoutRejectedException>()
            .WaitAndRetryAsync(
                options.MaxRetryCount,
                attempt => CalculateDelay(attempt, options),
                (exception, _, attempt, _) =>
                {
                    // Hook for future logging
                });

        var circuitBreakerPolicy = Policy
            .Handle<DbException>()
            .Or<TimeoutException>()
            .Or<TimeoutRejectedException>()
            .CircuitBreakerAsync(options.CircuitBreakerFailureThreshold, options.CircuitBreakerDuration);

        return new ResiliencePolicy(Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy));
    }

    public Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> action)
    {
        return _policy.ExecuteAsync(action);
    }

    private static TimeSpan CalculateDelay(int attempt, DatabaseResilienceOptions options)
    {
        var exponentialBackoff = options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var jitter = options.EnableJitter ? Random.Shared.NextDouble() * options.BaseDelay.TotalMilliseconds : 0;
        return TimeSpan.FromMilliseconds(exponentialBackoff + jitter);
    }
}
