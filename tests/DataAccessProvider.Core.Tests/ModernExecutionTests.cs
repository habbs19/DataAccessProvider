using System.Data;
using System.Data.Common;
using DataAccessProvider.Core;
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Resilience;
using DataAccessProvider.Core.Types;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessProvider.Core.Tests;

public partial class DatabaseTransactionTests
{
    [Fact]
    public async Task Streaming_DisposesOnEarlyExit_MappingFailureAndCancellation()
    {
        var provider = new Provider(() => new());
        await using var client = new DatabaseClient<Marker>(provider);
        await foreach (var person in client.StreamAsync<Person>(DatabaseCommand.Text("read"))) { Assert.Equal("Ada", person.Name); break; }
        Assert.True(provider.Connections[0].IsDisposed); Assert.True(provider.Connections[0].Commands[0].IsDisposed);
        await Assert.ThrowsAsync<MappingException>(async () => { await foreach (var person in client.StreamAsync<StrictPerson>(DatabaseCommand.Text("read"))) { } });
        Assert.True(provider.Connections[1].IsDisposed);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => { await foreach (var person in client.StreamAsync<Person>(DatabaseCommand.Text("read"), cancellation.Token)) { } });
        Assert.Equal(2, provider.Connections.Count);
        await Assert.ThrowsAsync<NotSupportedException>(async () => { await foreach (var person in client.StreamAsync<Person>(DatabaseCommand.Text("read").WithOutput("out", DataAccessDbType.Int32))) { } });
    }
    private sealed class StrictPerson { public int Name { get; set; } }

    [Fact]
    public async Task LegacyRouting_CancellationReachesSource_AndCannotBeSilentlyIgnored()
    {
        var services = new ServiceCollection().AddDataAccessProviderCore();
        await using var host = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope = host.CreateAsyncScope(); var router = scope.ServiceProvider.GetRequiredService<IDataSourceProvider>();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => router.ExecuteNonQueryAsync(new JsonFileSourceParams { FilePath = "does-not-exist" }, cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => router.ExecuteReaderAsync(new StaticCodeParams { Content = "data" }, cancellation.Token));
        var typedRouter=scope.ServiceProvider.GetRequiredService<IDataSourceProvider<StaticCodeParams>>();
        await Assert.ThrowsAsync<OperationCanceledException>(()=>typedRouter.WithCancellation().ExecuteReaderAsync<Person>(new StaticCodeParams {Content=new Person()},cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => new JsonFileClient().ReadJsonAsync<Person>("does-not-exist", cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task CustomRetryClassifier_CannotRetryPermanentArgumentFailures()
    {
        var attempts = 0; var policy = new BasicResiliencePolicy(3, TimeSpan.FromSeconds(1), _ => true);
        await Assert.ThrowsAsync<ArgumentException>(() => policy.ExecuteAsync<int>(_ => { attempts++; throw new ArgumentException(); }));
        Assert.Equal(1, attempts);
    }

    [Fact]
    public void RepeatedRegistration_IsIdempotent_AndConflictErrorsOmitConnectionSecrets()
    {
        var services = new ServiceCollection(); Assert.True(ProviderRegistration.Ensure(services, typeof(Marker), "secret-first"));
        Assert.False(ProviderRegistration.Ensure(services, typeof(Marker), "secret-first"));
        var failure = Assert.Throws<InvalidOperationException>(() => ProviderRegistration.Ensure(services, typeof(Marker), "secret-second"));
        Assert.DoesNotContain("secret", failure.Message);
    }
    private sealed class Marker;
    private sealed class TransientFailure : DbException { public override bool IsTransient => true; }
    private sealed class Provider(Func<RecordingConnection> create, bool throwParameter = false) : IRelationalProvider<Marker>
    {
        public List<RecordingConnection> Connections { get; } = [];
        public DatabaseCapabilities Capabilities => new();
        public DbConnection CreateConnection() { var connection = create(); Connections.Add(connection); return connection; }
        public DbCommand CreateCommand(DbConnection connection, DatabaseCommand definition)
        { var command = new RecordingCommand((RecordingConnection)connection); ((RecordingConnection)connection).Commands.Add(command); return command; }
        public DbParameter CreateParameter(DbCommand command, DatabaseParameter definition)
        {
            if (throwParameter) throw new ArgumentException("invalid parameter");
            return new RecordingParameter
            {
                ParameterName = definition.Name,
                Value = definition.Value,
                Direction = definition.Direction switch
                {
                    DataAccessParameterDirection.Output => ParameterDirection.Output,
                    DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue,
                    _ => ParameterDirection.Input
                }
            };
        }
    }
    [Fact]
    public async Task ModernQuery_HasStableRows_AndLegacyOverloadsAcceptTheirActualParameterTypes()
    {
        var provider = new Provider(() => new()); await using var client = new DatabaseClient<Marker>(provider);
        Assert.Equal("Ada", Assert.Single((await client.QueryAsync<Person>(DatabaseCommand.Text("read"))).Rows).Name);
        var source = new FakeSource(new());
        var request = new FakeSourceParams<Person> { Query = "read" };
        Assert.Same(request, await source.ExecuteReaderAsync<Person, FakeSourceParams<Person>>(request));
        Assert.Equal("Ada", Assert.Single(request.Value!).Name);
        IDataSource<FakeSourceParams> typed = new FakeSource(new());
        Assert.Equal("Ada", Assert.Single((await typed.ExecuteReaderAsync<Person>(new() { Query = "read" })).Value!).Name);
        Assert.All(provider.Connections, connection => Assert.True(connection.IsDisposed));
    }
    [Fact]
    public async Task ReusedTypedRequest_IsClearedWhenTheNextReadIsEmpty()
    {
        var table = new DataTable(); table.Columns.Add("Name", typeof(string)); table.Rows.Add("Ada");
        var source = new FakeSource(new() { ReaderTable = table }); var request = new FakeSourceParams<Person> { Query = "read" };
        await source.ExecuteReaderAsync<Person>(request); Assert.Single(request.Value!);
        table.Rows.Clear(); await source.ExecuteReaderAsync<Person>(request); Assert.Empty(request.Value!);
    }
    [Fact]
    public async Task ModernOutputs_AreSeparateFromTheImmutableRequest()
    {
        var provider = new Provider(() => new()); await using var client = new DatabaseClient<Marker>(provider);
        var command = DatabaseCommand.StoredProcedure("output").WithOutput("out", DataAccessDbType.Int32).WithReturnValue("return", DataAccessDbType.Int32);
        var result = await client.ExecuteAsync(command);
        Assert.Equal(42, result.Outputs["out"]); Assert.Equal(9, result.ReturnValue); Assert.Null(command.Parameters[0].Value);
        Assert.True(provider.Connections[0].Commands[0].IsDisposed);
    }
    [Fact]
    public async Task RetryRequiresSafety_UsesFreshResources_AndDoesNotRetryPermanentErrors()
    {
        var attempt = 0; var provider = new Provider(() => new() { CommandFailure = ++attempt < 3 ? new TransientFailure() : null });
        await using var client = new DatabaseClient<Marker>(provider, new() { MaxRetries = 3, InitialDelay = TimeSpan.Zero, MaxDelay = TimeSpan.Zero });
        await Assert.ThrowsAsync<TransientFailure>(() => client.ExecuteAsync(DatabaseCommand.Text("write")));
        Assert.Single(provider.Connections);
        attempt = 0; provider.Connections.Clear();
        await client.ExecuteAsync(DatabaseCommand.Text("safe write").WithRetrySafety(RetrySafety.Idempotent));
        Assert.Equal(3, provider.Connections.Count);
        Assert.All(provider.Connections, connection => { Assert.True(connection.IsDisposed); Assert.True(connection.Commands[0].IsDisposed); });
        var permanent = new Provider(() => new() { CommandFailure = new ArgumentException() });
        await using var other = new DatabaseClient<Marker>(permanent, new() { MaxRetries = 3 });
        await Assert.ThrowsAsync<ArgumentException>(() => other.ExecuteAsync(DatabaseCommand.Text("write").WithRetrySafety(RetrySafety.Idempotent)));
        Assert.Single(permanent.Connections);
    }
    [Fact]
    public async Task Cancellation_StopsWork_AndPreCancelledCallsCreateNoConnection()
    {
        var provider = new Provider(() => new() { CommandDelay = TimeSpan.FromSeconds(10) }); await using var client = new DatabaseClient<Marker>(provider);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ExecuteAsync(DatabaseCommand.Text("write"), new CancellationToken(true)));
        Assert.Empty(provider.Connections);
        using var token = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ExecuteAsync(DatabaseCommand.Text("write"), token.Token));
        Assert.True(provider.Connections[0].IsDisposed); Assert.True(provider.Connections[0].Commands[0].IsDisposed);
    }
    [Fact]
    public async Task ConfigurationFailure_DisposesCommandAndTransaction_AndPreservesOriginalFailure()
    {
        var provider = new Provider(() => new(), throwParameter: true); await using var client = new DatabaseClient<Marker>(provider);
        await Assert.ThrowsAsync<ArgumentException>(() => client.ExecuteInTransactionAsync(async (session, ct) =>
            await session.ExecuteAsync(DatabaseCommand.Text("write").WithParameter("id", DataAccessDbType.Int32, 1), ct)));
        var connection = Assert.Single(provider.Connections);
        Assert.True(connection.Commands[0].IsDisposed); Assert.True(connection.IsDisposed);
        Assert.Equal(1, connection.Transaction.RollbackCount); Assert.True(connection.Transaction.IsDisposed);
    }
    [Fact]
    public async Task ModernTransaction_CommitsSerializesAndRejectsEscapedSession()
    {
        var provider = new Provider(() => new() { CommandDelay = TimeSpan.FromMilliseconds(10) }); await using var client = new DatabaseClient<Marker>(provider);
        IDatabaseSession? escaped = null;
        await client.ExecuteInTransactionAsync(async (session, ct) =>
        { escaped = session; await Task.WhenAll(Enumerable.Range(0, 3).Select(_ => session.ExecuteAsync(DatabaseCommand.Text("write"), ct))); });
        var connection = Assert.Single(provider.Connections);
        Assert.Equal(1, connection.Transaction.CommitCount); Assert.Equal(1, connection.MaximumConcurrentCommands);
        await Assert.ThrowsAsync<InvalidOperationException>(() => escaped!.ExecuteAsync(DatabaseCommand.Text("late")));
    }
    [Fact]
    public async Task StrictMapping_IsValueFree_AndCleansUp()
    {
        var table = new DataTable(); table.Columns.Add("Id", typeof(string)); table.Rows.Add("SECRET-SENTINEL");
        var provider = new Provider(() => new() { ReaderTable = table }); await using var client = new DatabaseClient<Marker>(provider);
        var error = await Assert.ThrowsAsync<MappingException>(() => client.QueryAsync<NumberRow>(DatabaseCommand.Text("read")));
        Assert.DoesNotContain("SECRET-SENTINEL", error.ToString()); Assert.Null(error.InnerException);
        Assert.True(provider.Connections[0].IsDisposed); Assert.True(provider.Connections[0].Commands[0].IsDisposed);
    }
    private sealed class NumberRow { public int Id { get; set; } }
    [Fact]
    public async Task Mapper_HandlesModernDatesAndIgnoresIndexers()
    {
        var table = new DataTable(); table.Columns.Add("Date", typeof(DateTime)); table.Columns.Add("Time", typeof(TimeSpan));
        table.Rows.Add(new DateTime(2026, 10, 5), TimeSpan.FromHours(12));
        var provider = new Provider(() => new() { ReaderTable = table }); await using var client = new DatabaseClient<Marker>(provider);
        var row = Assert.Single((await client.QueryAsync<DateRow>(DatabaseCommand.Text("read"))).Rows);
        Assert.Equal(new DateOnly(2026, 10, 5), row.Date); Assert.Equal(new TimeOnly(12, 0), row.Time);
    }
    private sealed class DateRow { public DateOnly Date { get; set; } public TimeOnly Time { get; set; } public string this[int index] { get => ""; set { } } }
    [Fact]
    public async Task StrictDatabaseNull_FailsClearly()
    {
        var table = new DataTable(); table.Columns.Add("Id", typeof(int)); table.Rows.Add(DBNull.Value);
        var provider = new Provider(() => new() { ReaderTable = table }); await using var client = new DatabaseClient<Marker>(provider);
        await Assert.ThrowsAsync<MappingException>(() => client.QueryAsync<NumberRow>(DatabaseCommand.Text("read")));
    }
    [Fact]
    public async Task LegacyPolicy_IsForwardedOnlyForExplicitlyRetrySafeWork()
    {
        var policy = new CountingResiliencePolicy(); var source = new FakeSource(new(), policy);
        await source.ExecuteNonQueryAsync(new FakeSourceParams { Query = "write" }); Assert.Equal(0, policy.ExecutionCount);
        await source.ExecuteNonQueryAsync(new FakeSourceParams { Query = "safe write", RetrySafety = RetrySafety.Idempotent }); Assert.Equal(1, policy.ExecutionCount);
    }
    [Fact]
    public async Task BasicPolicy_DoesNotRetryArgumentErrors_AndValidatesOptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BasicResiliencePolicy(-1, TimeSpan.FromSeconds(1)));
        var policy = new BasicResiliencePolicy(3, TimeSpan.FromSeconds(1)); var attempts = 0;
        await Assert.ThrowsAsync<ArgumentException>(() => policy.ExecuteAsync<int>(_ => { attempts++; throw new ArgumentException(); })); Assert.Equal(1, attempts);
    }
    [Fact]
    public async Task FileAndStaticSources_SupportTypedValues_AndActualEncodedByteCounts()
    {
        var path = Path.Combine(Path.GetTempPath(), "dap-" + Guid.NewGuid().ToString("N"));
        try
        {
            var files = new JsonFileClient();
            await Assert.ThrowsAsync<FileNotFoundException>(() => files.WriteTextAsync(path, "é"));
            var written = await files.WriteTextAsync(path, "é", FileWriteMode.CreateNew); Assert.Equal(2, written.BytesWritten);
            await Assert.ThrowsAsync<IOException>(() => files.WriteTextAsync(path, "x", FileWriteMode.CreateNew));
            var source = new StaticCodeSource(); var request = new StaticCodeParams<Person> { Content = new Person { Name = "Ada" } };
            Assert.Equal("Ada", Assert.Single((await source.ExecuteReaderAsync<Person>(request)).Value!).Name);
        }
        finally { File.Delete(path); }
    }
    private sealed class ScopedSource : StaticCodeSource, IDisposable { public bool Disposed { get; private set; } public void Dispose() => Disposed = true; }
    private static class Left { public sealed class SameNameParams : BaseDataSourceParams; }
    private static class Right { public sealed class SameNameParams : BaseDataSourceParams; }
    [Fact]
    public void TypeKeyedFactory_SeparatesEqualSimpleNames_AndResolvesConcurrently()
    {
        var services = new ServiceCollection().AddDataAccessProviderCore(); services.AddScoped<ScopedSource>();
        services.AddSingleton(new DataSourceRegistration(typeof(Left.SameNameParams), typeof(ScopedSource)));
        services.AddSingleton(new DataSourceRegistration(typeof(Right.SameNameParams), typeof(StaticCodeSource)));
        using var host = services.BuildServiceProvider(); using var scope = host.CreateScope(); var factory = scope.ServiceProvider.GetRequiredService<IDataSourceFactory>();
        var left = factory.CreateDataSource(new Left.SameNameParams()); var right = factory.CreateDataSource(new Right.SameNameParams()); Assert.NotSame(left, right);
        Parallel.For(0, 100, _ => Assert.Same(left, factory.CreateDataSource(new Left.SameNameParams())));
        Assert.Equal(2, factory.GetRegisteredDataSources().Keys.Count(x => x.Contains("SameNameParams")));
    }
    [Fact]
    public async Task DriverSuccessAfterCancellation_CannotBeReportedAsSuccessfulWork()
    {
        var provider = new Provider(() => new() { CommandDelay = TimeSpan.FromMilliseconds(100), IgnoreCancellation = true });
        await using var client = new DatabaseClient<Marker>(provider); using var cancellation = new CancellationTokenSource(20);
        await Assert.ThrowsAsync<OperationCanceledException>(() => client.ScalarAsync<int>(DatabaseCommand.Text("read"), cancellation.Token));
        Assert.True(provider.Connections[0].IsDisposed); Assert.True(provider.Connections[0].Commands[0].IsDisposed);
    }
    [Fact]
    public async Task Diagnostics_ExcludeSqlAndValues()
    {
        var activities = new System.Collections.Concurrent.ConcurrentBag<System.Diagnostics.Activity>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == DataAccessDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> options) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        var provider = new Provider(() => new()); await using var client = new DatabaseClient<Marker>(provider);
        await client.ExecuteAsync(DatabaseCommand.Text("SQL-SECRET").WithParameter("name", DataAccessDbType.String, "VALUE-SECRET"));
        Assert.Contains(activities, x => x.GetTagItem("db.provider")?.ToString() == nameof(Marker));
        Assert.All(activities.SelectMany(x => x.TagObjects), tag => { Assert.DoesNotContain("SECRET", tag.Value?.ToString() ?? ""); });
    }
    [Fact]
    public void Factory_UsesCallerScope_AndDictionaryMutationDoesNotAlterMappings()
    {
        var services = new ServiceCollection().AddDataAccessProviderCore(); services.AddScoped<ScopedSource>();
        services.AddSingleton(new DataSourceRegistration(typeof(StaticCodeParams), typeof(ScopedSource)));
        using var host = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        ScopedSource source;
        using (var scope = host.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDataSourceFactory>();
            source = (ScopedSource)factory.CreateDataSource(new StaticCodeParams()); Assert.False(source.Disposed);
            factory.GetRegisteredDataSources().Clear(); Assert.Same(source, factory.CreateDataSource(new StaticCodeParams()));
            Assert.IsType<StaticCodeParams>(factory.CreateParams<StaticCodeParams>());
        }
        Assert.True(source.Disposed);
    }
}
