namespace DataAccessProvider.Core.Types;

public sealed class DatabaseResilienceOptions
{
    public static DatabaseResilienceOptions Default { get; } = new();

    /// <summary>
    /// Maximum retry attempts when a transient failure occurs.
    /// </summary>
    public int MaxRetryCount { get; init; } = 3;

    /// <summary>
    /// Base delay used for exponential backoff between retries.
    /// </summary>
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// The number of consecutive failures allowed before opening the circuit breaker.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; init; } = 5;

    /// <summary>
    /// The duration to keep the circuit open after the threshold is reached.
    /// </summary>
    public TimeSpan CircuitBreakerDuration { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Maximum number of database connections allowed in the pool.
    /// </summary>
    public int MaxPoolSize { get; init; } = 20;

    /// <summary>
    /// Adds randomness to retry delays to avoid thundering herd issues.
    /// </summary>
    public bool EnableJitter { get; init; } = true;
}
