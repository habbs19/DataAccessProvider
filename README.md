# DataAccessProvider

.NET 10 data access with one chosen provider package. Drivers, Core, configuration and dependency injection flow transitively. Version 1.4.0 is an unpublished local candidate until release validation and publication are completed.

| Database | Package | Namespace | Client | Registration |
|---|---|---|---|---|
| SQL Server | DataAccessProvider.MSSQL | DataAccessProvider.MSSQL | SqlServerClient | AddDataAccessProviderMSSQL |
| MySQL | DataAccessProvider.MySql | DataAccessProvider.MySql | MySqlClient | AddDataAccessProviderMySql |
| PostgreSQL | DataAccessProvider.PostgreSql | DataAccessProvider.Postgres | PostgreSqlClient | AddDataAccessProviderPostgres |
| MongoDB | DataAccessProvider.MongoDB | DataAccessProvider.MongoDB | MongoDocumentClient | AddDataAccessProviderMongoDB |
| Oracle | DataAccessProvider.Oracle | DataAccessProvider.Oracle | OracleClient | AddDataAccessProviderOracle |
| Snowflake | DataAccessProvider.Snowflake | DataAccessProvider.Snowflake | SnowflakeClient | AddDataAccessProviderSnowflake |

Pack the candidate and install only the provider you need from the local feed. Published releases use NuGet.org. A Core namespace import does not require another direct package reference.

```powershell
# Run in this repository with PowerShell 7; restores both public and local dependencies.
dotnet restore DataAccessProvider.sln --locked-mode
dotnet build DataAccessProvider.sln -c Release --no-restore
./eng/Verify-Packages.ps1 -ScratchRoot ./artifacts/validation
# Run in a separate console project outside this repository's central package configuration.
# Copy artifacts/validation/consumers/local-MSSQL/NuGet.Config into that project first.
dotnet add package DataAccessProvider.MSSQL --version 1.4.0
```

## SQL quick start

Supply a connection string securely from your application's configuration. Constructor-created clients are owned by their caller. Queries return collections for zero, one or many rows; commands and scalars have separate results.

```csharp
using DataAccessProvider.Core;
using DataAccessProvider.Core.Types;
using DataAccessProvider.MSSQL;

await using var database = new SqlServerClient(Environment.GetEnvironmentVariable("DATABASE_CONNECTION")!);
using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await database.QueryAsync<User>(
    DatabaseCommand.Text("SELECT Id, Name FROM Users WHERE Id=@id")
        .WithParameter("@id", DataAccessDbType.Int32, 1), cancellation.Token);
foreach (var user in result.Rows) Console.WriteLine(user.Name);
public sealed class User { public int Id { get; set; } public string? Name { get; set; } }
```

Standalone DI works with the same one provider reference. `Add…` completes registration; no `Use…` call is required. Resolve clients from an application scope.

```csharp
using DataAccessProvider.Core;
using DataAccessProvider.MSSQL;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection().AddDataAccessProviderMSSQL("Server=localhost;Database=example;Integrated Security=true;TrustServerCertificate=true");
await using var host = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
await using var scope = host.CreateAsyncScope();
var database = scope.ServiceProvider.GetRequiredService<IDatabaseClient<SqlServer>>();
```

## Contracts and capabilities

Commands default to Text, a cooperative 30-second timeout and no wrapper retries. Explicitly select StoredProcedure or infinite timeout when needed. SQL and parameter values are separate; callers must validate/quote dynamic identifiers. Immutable command definitions do not freeze arbitrary native object values; callers must not mutate those during execution.

Strict mapping rejects invalid conversions, database null into non-nullable members and duplicate dictionary column names. Use writable POCOs or the explicit DbDataReader mapper overload; automatic record-constructor mapping is not promised.

`StreamAsync` optionally streams the first result set without retaining all mapped rows. An `await foreach` early exit disposes its reader, command and connection. It does not retry, expose outputs, or join managed transaction sessions; use buffered queries for those contracts. Dispose manually obtained enumerators and do not let enumeration escape a client scope.

`QueryResult.Rows` always contains a collection. ScalarState distinguishes NoRow, Null and Value. Outputs are captured after readers close. Native access callbacks own commands/readers they create and must complete before the library-owned connection/session ends.

Transactions serialize commands, commit after successful callbacks, roll back on failure/cancellation and reject escaped sessions. A failed commit has an unknown outcome and is never automatically replayed. DDL can implicitly commit on some databases; the callback does not make such statements atomic.

Wrapper retries require both configured MaxRetries and an explicit ReadOnly/Idempotent declaration. Unknown/permanent errors, cancellation, ordinary writes and transaction operations are not retried. MongoDB relies on driver-managed retryable writes rather than an additional wrapper write retry loop. Cancellation is cooperative and driver exception types can differ; cancellation does not prove a write never occurred.

Oracle uses named binding in the modern client. Variable-width output parameters need an appropriate capacity. Snowflake uses text CALL commands and does not expose the universal stored-procedure/output-parameter contract; account-backed integration remains pending. Advanced bulk copy, arrays, ref cursors, aggregation and stages use each provider's typed native surface.

## Validation and migration

Run `dotnet restore --locked-mode`, `dotnet test -c Release --no-restore`, and `pwsh ./eng/Verify-Packages.ps1`. Package checks are independent from database checks. See [migration examples](docs/migration.md), [verification](docs/implementation.md), and the historical [review](review/DataAccessProvider-review.md).

Legacy APIs remain available throughout 1.4. Their mutable request instances are not safe for concurrent reuse. The later 2.0 release retires them only after a stable 1.4 migration release.
