using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace DataAccessProvider.Core;

/// <summary>Value-free instrumentation shared by built-in and custom providers.</summary>
public static class DataAccessDiagnostics
{
    public const string SourceName = "DataAccessProvider";
    internal static readonly ActivitySource Activities = new(SourceName);
    private static readonly Meter Meter = new(SourceName);
    internal static readonly Histogram<double> Duration = Meter.CreateHistogram<double>("dataaccess.operation.duration", "ms");
    internal static readonly Counter<long> Attempts = Meter.CreateCounter<long>("dataaccess.operation.attempts");

    public static async Task<T> MeasureAsync<T>(string provider, string operation, Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var activity = Activities.StartActivity(operation); activity?.SetTag("db.provider", provider);
        var started = Stopwatch.GetTimestamp(); Attempts.Add(1, new KeyValuePair<string, object?>("db.provider", provider));
        try { return await action().ConfigureAwait(false); }
        catch { activity?.SetStatus(ActivityStatusCode.Error); throw; }
        finally { Duration.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds, new KeyValuePair<string, object?>("db.provider", provider)); }
    }
}
