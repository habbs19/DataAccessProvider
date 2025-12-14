namespace DataAccessProvider.Core.Resilience;

public class ResilienceOptions
{
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Timeout per attempt in seconds.
    /// </summary>
    public int PerAttemptTimeoutSeconds { get; set; } = 30;
}