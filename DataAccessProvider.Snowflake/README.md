# DataAccessProvider.Snowflake

.NET 10 provider. Install only DataAccessProvider.Snowflake; Core, the driver and DI implementation flow transitively. Version 1.4.0 is an unpublished local candidate. Pack the repository libraries to a local feed for candidate consumption; published releases use NuGet.org.

Construction does not connect. Obtain real credentials securely from application configuration; placeholder values below demonstrate registration only.

~~~csharp
using DataAccessProvider.Core;
using DataAccessProvider.Snowflake;
using Microsoft.Extensions.DependencyInjection;

await using var direct = new SnowflakeClient("account=example;user=example;password=placeholder;db=example;warehouse=example");
var services = new ServiceCollection().AddDataAccessProviderSnowflake("account=example;user=example;password=placeholder;db=example;warehouse=example");
await using var host = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
await using var scope = host.CreateAsyncScope();
var client = scope.ServiceProvider.GetRequiredService<IDatabaseClient<SnowflakeDatabase>>();
~~~

Add completes registration; no Use call is necessary. Constructor-created clients are caller-owned. Host clients belong to their application scope. Use tokens for ordinary operations and dispose native commands/readers/cursors created in advanced callbacks.

Relational commands default to Text, a cooperative 30-second timeout and no wrapper retries. Query results always contain a collection. Output values are available after reader closure. Driver-supported transactions serialize commands and never replay ambiguous commits. MongoDB offers typed CRUD/expressions and driver-managed retryable writes; it is not a SQL command facade.

Oracle modern commands bind by name. Variable-width outputs require capacity. Snowflake requires text CALL and does not promise ADO.NET output parameters; account-backed integration remains pending. Advanced provider types remain transitive and accessible through native callbacks.

Legacy source/parameter APIs remain compatibility adapters during 1.4. See the repository README and docs/migration.md for queries, commands, procedures, transactions, cancellation, advanced access and 2.0 migration.
