using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DataAccessProvider.Core.Extensions;

namespace DataAccessProvider.Core;

public static class ProviderRegistration
{
    private sealed record Settings(int Retries, TimeSpan InitialDelay, TimeSpan MaxDelay, Func<Exception, bool>? Classifier, Func<TimeSpan, CancellationToken, Task> Delay);
    private sealed record Configuration(Type Marker, string ConnectionString, Settings? Options);
    public static bool Ensure(IServiceCollection services, Type marker, string connectionString)
        => Ensure(services, marker, connectionString, null);
    public static bool Ensure(IServiceCollection services, Type marker, string connectionString, DatabaseClientOptions? options)
    {
        var settings = options is null ? null : new Settings(options.MaxRetries, options.InitialDelay, options.MaxDelay, options.IsTransient, options.DelayAsync);
        var existing = services.Select(x => x.ImplementationInstance).OfType<Configuration>().FirstOrDefault(x => x.Marker == marker);
        if (existing is not null)
        {
            if (existing.ConnectionString != connectionString || existing.Options != settings) throw new InvalidOperationException($"Conflicting configuration for {marker.Name}.");
            return false;
        }
        services.AddSingleton(new Configuration(marker, connectionString, settings)); return true;
    }
    public static DatabaseClientOptions Snapshot(DatabaseClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options); ArgumentNullException.ThrowIfNull(options.DelayAsync);
        if (options.MaxRetries < 0 || options.InitialDelay < TimeSpan.Zero || options.MaxDelay < options.InitialDelay) throw new ArgumentOutOfRangeException(nameof(options));
        return new() { MaxRetries = options.MaxRetries, InitialDelay = options.InitialDelay, MaxDelay = options.MaxDelay, IsTransient = options.IsTransient, DelayAsync = options.DelayAsync };
    }
    public static IServiceCollection AddDatabaseProvider<TMarker, TAdapter>(this IServiceCollection services, DatabaseClientOptions? options = null)
        where TAdapter : class, IRelationalProvider<TMarker>
    {
        options = Snapshot(options ?? new());
        services.AddDataAccessProviderCore();
        services.TryAddSingleton<IRelationalProvider<TMarker>, TAdapter>();
        services.TryAddScoped<IDatabaseClient<TMarker>>(sp => new DatabaseClient<TMarker>(sp.GetRequiredService<IRelationalProvider<TMarker>>(), options));
        return services;
    }
}
