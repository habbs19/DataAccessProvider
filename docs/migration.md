# Migration to the 1.4 API

Version 1.4 keeps the released execution APIs as compatibility adapters. A later 2.0 release will remove legacy mutable parameters/results, IDataSource/IDataSourceProvider/IDataSourceFactory, their execution base classes, convention discovery, runtime registration and Use methods. The new API must ship in a stable minor release first. No removal or publication has occurred yet.

| Legacy usage | Replacement |
|---|---|
| SourceParams.Query and mutable Parameters | Immutable DatabaseCommand/DatabaseParameter |
| ExecuteReaderAsync and request.Value | QueryAsync and result.Rows |
| ExecuteScalarAsync and request.Value | ScalarAsync and ScalarState/Value |
| ExecuteNonQueryAsync and AffectedRows | ExecuteAsync and CommandResult |
| Output values on parameter objects | Result.Outputs/ReturnValue |
| DataSourceProvider/factory routing | IDatabaseClient with a provider marker |
| Add followed by Use | Add alone; resolve within a scope |
| Custom IDataSource implementations | IRelationalProvider for SQL; IDocumentClient or IValueSource for other sources |
| JsonFileSource mutable params | JsonFileClient with explicit write mode |
| StaticCodeSource mutable params | StaticValueSource with explicit transformation |

## Commands, procedures, transactions and cancellation

This SQL Server example uses one provider package and a fixture database containing Users and review_output. Do not run it against production as an example. The documented procedure assigns @out=42 and returns 9.

```csharp
using DataAccessProvider.Core;
using DataAccessProvider.Core.Types;
using DataAccessProvider.MSSQL;

await using var database = new SqlServerClient(Environment.GetEnvironmentVariable("DATABASE_CONNECTION")!);
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var ct = cancellation.Token;
var count = await database.ScalarAsync<int>(DatabaseCommand.Text("SELECT COUNT(*) FROM Users"), ct);
if (count.State == ScalarState.Value) Console.WriteLine(count.Value);
var write = await database.ExecuteAsync(DatabaseCommand.Text("UPDATE Users SET Name=@name WHERE Id=@id")
    .WithParameter("@name", DataAccessDbType.String, "Ada", size: 100)
    .WithParameter("@id", DataAccessDbType.Int32, 1), ct);
var procedure = await database.ExecuteAsync(DatabaseCommand.StoredProcedure("review_output")
    .WithOutput("@out", DataAccessDbType.Int32).WithReturnValue("@return", DataAccessDbType.Int32), ct);
Console.WriteLine($"Output={procedure.Outputs["@out"]}; return={procedure.ReturnValue}");
await database.ExecuteInTransactionAsync(async (transaction, token) => {
    await transaction.ExecuteAsync(DatabaseCommand.Text("UPDATE Users SET Name=@name WHERE Id=1")
        .WithParameter("@name", DataAccessDbType.String, "Grace", size: 100), token);
}, cancellationToken: ct);
```

Stored procedure syntax and output support remain provider capabilities. PostgreSQL INOUT, MySQL OUT and Oracle anonymous-block outputs are separately tested. Snowflake examples use text CALL and account-backed tests are pending. Unsupported command kinds are rejected; driver limitations are not emulated.

## Documents

Use typed records/classes and expressions for ordinary document operations. Advanced pipelines, native update/projection definitions and sessions remain available through MongoDocumentClient.ExecuteNativeAsync; dispose cursors/sessions created inside that callback.

```csharp
using DataAccessProvider.MongoDB;

await using var documents = new MongoDocumentClient("mongodb://localhost/example");
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
await documents.InsertAsync("users", new User { Id = 1, Name = "Ada" }, cancellation.Token);
var users = await documents.FindAsync<User>("users", user => user.Id == 1, cancellationToken: cancellation.Token);
await documents.SetAsync<User, string>("users", user => user.Id == 1, user => user.Name, "Grace", cancellationToken: cancellation.Token);
await documents.DeleteAsync<User>("users", user => user.Id == 1, cancellationToken: cancellation.Token);
public sealed class User { public int Id { get; set; } public string Name { get; set; } = ""; }
```

## Behavior changes in 1.4

Empty typed results now clear earlier rows. Output/return values propagate after completion. Factory dictionary inspection is a snapshot; mutate registrations through the supported registration method during migration, not through the returned dictionary. Factory services are scoped, so singleton consumers must create and keep an application scope for the entire operation.

New commands use Text and finite timeout defaults; legacy defaults remain StoredProcedure and zero timeout. New mapping is strict; legacy dictionary conversion stays permissive. Mapping exceptions omit raw values, including value-bearing conversion exception messages. File operations preserve useful native I/O exception types; callers relying on the older generic wrapper should update exception handling.

Legacy SQL retries now require explicit RetrySafety on the request. A supplied policy is forwarded, but ordinary writes and transactions are not automatically replayed. MongoDB uses driver-managed retryable writes; the wrapper no longer retries all exceptions. Retry classifiers must positively identify transient errors. Mutable legacy requests require separate instances per concurrent operation.

MySQL's released five-argument AddJSONParams signatures are restored, including custom operationLabel/paramsLabel. Existing three-argument calls remain supported. MongoDB.Driver is upgraded to 3.12.0; applications using transitive native driver APIs must consult the driver migration guidance and test those calls.

## Custom providers and advanced access

Implement IRelationalProvider<TMarker> and register it using AddDatabaseProvider<TMarker,TAdapter>. Return new caller-owned logical connections. The shared engine owns connection/command/reader disposal, results, outputs, cancellation and transaction completion. Implement command validation and transient classification for your driver; unknown failures must remain non-transient.

For native relational work use SqlServerAdvanced, MySqlAdvanced, PostgreSqlAdvanced, OracleAdvanced or SnowflakeAdvanced with an IDatabaseSession. The callback gets a borrowed connection/transaction valid only inside that callback; it must dispose any commands/readers it creates, and must not close, commit or dispose the supplied connection/transaction. External/nested/distributed transaction attachment is outside the basic contract.

Modern mappers support writable POCOs or explicit reader delegates. Record-constructor mapping, exhaustive LINQ translation and full NativeAOT support are not promised. `StreamAsync` is an opt-in first-result-set API: dispose enumeration on early exit, complete it within the client scope, and use buffered queries when outputs, multiple results or managed transaction sessions are required. Streaming never retries partially delivered rows.

Legacy callers can import DataAccessProvider.Core.Extensions and pass cancellation through IDataSource/IDataSourceProvider extension methods. Custom legacy sources must implement ICancellableDataSource; unsupported custom sources fail explicitly instead of silently dropping the token. The published legacy interfaces retain their original abstract members.

Core emits DAP001 migration warnings for legacy execution contracts. Remove these uses from consumer applications before 2.0. Driver-specific parameter helpers and native driver types remain supported advanced capabilities; the retirement applies to the legacy execution hierarchy, factory and routing interfaces.
