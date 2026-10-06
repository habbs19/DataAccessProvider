# DataAccessProvider evidence-based review

**Reviewed commit:** 0fe75f74d5c2e2254f7b08ef70f44e623a926fd8  
**Inspection date:** October 5, 2026, America/Toronto  
**Baseline:** .NET 10; SDK 10.0.400; runtime 10.0.11; Windows x64 consumer processes, Linux amd64 disposable database containers.

This review changes only review artifacts and generated validation outputs. No production fix, package publication, or GitHub issue was created. Proposed code below is explicitly distinguished from existing APIs.

## 1. Executive assessment

**The drivers already flow transitively. The principal package defect is suppressed DI dependency metadata, followed by incorrect public overloads and inaccurate examples.** A plain .NET 10 application with one direct MSSQL package reference restores SqlClient and Core automatically, compiles, constructs the source, and executes parameterized SQL against a disposable SQL Server. The same basic SQL path works for MySQL and PostgreSQL. Installing SqlClient separately is unnecessary. Both fresh and published MSSQL 1.3.7 declare the appropriate dependencies; the [published NuGet metadata](https://www-1.nuget.org/packages/DataAccessProvider.MSSQL/) agrees with the extracted package.

The broader promise of straightforward consumption is currently incomplete:

- MSSQL, MySQL, and MongoDB registration fails to compile in clean consumers because the public registration signatures reference DI Abstractions 10 while package metadata supplies an older version or none. PostgreSQL succeeds through an incidental Npgsql dependency. Core public signature loading also fails in five of the six local provider consumers.
- Typed SQL reads work through one overload, while two other public paths throw. Empty typed results can retain a previous query's rows. Output and return parameters do not reach the caller.
- MongoDB's default find throws before executing the query. Its typed conversion loses filters, and its typed result path throws after materialization.
- Oracle is locally packable and reachable in a disposable fixture, but its default parameter size causes ordinary parameterized commands to throw. Explicit sizes allow read, scalar, and write checks to succeed.
- Factory resolution returns scoped services after disposing their owning scope. Relational resilience policies are discarded by the generic base constructor. Enabling them without repairing retry ownership would expose another defect.
- Existing tests pass, but they miss these package and API paths. Documentation contains wrong method names, a wrong PostgreSQL package ID, invalid parameter lists, and unsupported resilience settings.

There is a useful foundation to preserve. Provider-specific type mapping is kept beside each driver; ordinary relational signatures use BCL connection/command types and neutral parameters. Connections and readers are ordinarily owned per operation. Managed SQL transactions serialize commands, reject escaped executors, roll back on cancellation, preserve rollback failures, and report an unknown commit outcome without automatically retrying it. The mapper already caches model setters and uses reader ordinals.

**Readiness:** workable for deliberately chosen SQL constructor-based paths; unsuitable for an unqualified claim that all documented provider workflows work with one package. PostgreSQL has the cleanest package registration closure, but shares the execution defects. MongoDB needs correctness fixes before ordinary reads are dependable. Oracle and Snowflake should be described as local projects pending a defined release/support contract.

Evidence: [package matrix][e-consumers], [SQL Server results][e-sql], [PostgreSQL results][e-pg], [MySQL results][e-mysql], [MongoDB results][e-mongo], [Oracle results][e-oracle], [focused reproductions][e-repro].

## 2. Baseline, inventory, and dependency map

### Scope and evaluated properties

The solution, seven libraries, console example, both test projects, repository instructions, editor configuration, package metadata, and both workflows were inspected. No applicable AGENTS.md, global.json, Directory.Build.props/targets, Directory.Packages.props, or package lock files were found. The initial tracked tree was clean. The [source manifest][e-source] records tracked source/configuration/documentation hashes and line counts; [evaluated properties][e-baseline] record MSBuild values and reference metadata.

All seven libraries evaluate to net10.0, IsPackable=true, Nullable=enable, IncludeBuildOutput=true, GenerateAssemblyInfo=true, SuppressDependenciesWhenPacking=false, and TreatWarningsAsErrors=false. No explicit IncludeAssets or ExcludeAssets restriction removes the drivers. Core and several providers explicitly suppress configuration/DI references with PrivateAssets=all. The nuspec exclusion of Build,Analyzers is ordinary dependency metadata and does not suppress runtime assemblies.

RepositoryCommit is empty during the property-only evaluation; packing subsequently obtains source-control provenance. All fresh packages contain the reviewed SHA. Oracle/Snowflake still lack a repository URL and meaningful package metadata. These distinctions matter when comparing evaluated properties with actual package output.

Release restore/build succeeded. The build reports seven warnings, including MongoDB dependency advisories, the hidden resilience member, and the Snowflake DI assembly conflict. All **42 existing tests passed: 31 Core xUnit tests and 11 MSTest tests**. The MSSQL test uses a mocked factory/source and does not validate a database connection. [Build][e-build], [tests][e-tests], [test source][s-test-mssql].

### Versioned package inventory

Versions in this table are the source package versions and direct dependency minimums, not a promise that every downstream application will resolve the same transitive graph.

| Package / purpose | Fresh version | Accessible latest stable | Direct fresh nuspec dependencies | Distribution |
|---|---:|---:|---|---|
| DataAccessProvider.Core: contracts, routing, mapping, resilience, file/static sources | 1.3.7 | 1.3.7 | Configuration.Binder 10.0.1 | Published; included in release workflow |
| DataAccessProvider.MSSQL: SQL Server | 1.3.7 | 1.3.7 | Core 1.3.7; Microsoft.Data.SqlClient 6.1.3 | Published; release workflow |
| DataAccessProvider.MySql: MySQL | 1.3.4 | 1.3.4 | Core 1.3.7; MySqlConnector 2.5.0; Microsoft.AspNetCore.Http.Abstractions 2.3.9 | Published; release workflow |
| DataAccessProvider.PostgreSql: PostgreSQL; project/assembly/namespace use Postgres | 1.3.4 | 1.3.4 | Core 1.3.7; Npgsql 10.0.1; Configuration.Abstractions 10.0.1 | Published; release workflow |
| DataAccessProvider.MongoDB: document operations | 1.3.3 | 1.3.3 | Core 1.3.7; MongoDB.Driver 3.5.2 | Published; release workflow |
| DataAccessProvider.Oracle: Oracle | 1.0.0, SDK default | Unavailable: NuGet flat-container index returned 404 | Core 1.3.7; Oracle.ManagedDataAccess.Core 23.26.0 | Locally packable; absent from release workflow |
| DataAccessProvider.Snowflake: Snowflake | 1.0.0, SDK default | Unavailable: index returned 404 | Core 1.3.7; Snowflake.Data 5.3.0; AWSSDK.Core 4.0.3.8 | Locally packable; absent from release workflow |

All expose only a net10.0 library asset group. Supporting earlier application frameworks would be a separate compatibility decision; this review retains .NET 10. A 404 establishes that the public index was unavailable under those IDs, not that no private or historical distribution can exist.

All seven projects were explicitly packed into a fresh temporary feed. Packages contain their provider DLL, with Core represented by a dependency rather than embedded copies. Four published provider packages and Core were downloaded separately; their matching source versions were also their accessible latest stable versions. Published packages carry a signature file. Hashes, exact file lists, all dependency groups, and provenance are in [package inventory][e-inventory] and the extracted nuspec files.

MSSQL, MySQL, PostgreSQL, and MongoDB include a package readme and MIT expression. Core includes README.md as a file but does not declare PackageReadmeFile or a license expression. Oracle/Snowflake include no readme and use default author/description metadata. The root repository license is MIT; the recommendation is to carry accurate existing license metadata into packages, not invent a different license.

### Dependency relationships

~~~mermaid
flowchart TD
    App["Consumer: ONE chosen provider PackageReference"]
    App --> SQL["MSSQL 1.3.7"]
    App --> MY["MySql 1.3.4"]
    App --> PG["PostgreSql 1.3.4"]
    App --> MO["MongoDB 1.3.3"]
    App --> OR["Oracle 1.0.0 local"]
    App --> SF["Snowflake 1.0.0 local"]
    SQL --> Core["Core 1.3.7"]
    MY --> Core
    PG --> Core
    MO --> Core
    OR --> Core
    SF --> Core
    Core --> Binder["Configuration.Binder 10.0.1"]
    Binder --> Config["Configuration / Abstractions 10.0.1"]
    Core -. "compiled reference suppressed in nuspec" .-> DI10["DI Abstractions 10.0.1"]
    SQL --> SqlClient["SqlClient 6.1.3"]
    SqlClient --> SNI["SNI.runtime 6.0.2"]
    SqlClient --> Cache["Caching.Memory 9.0.4"]
    Cache --> DI9["DI Abstractions 9.0.4"]
    MY --> MyDriver["MySqlConnector 2.5.0"]
    MyDriver --> Log8["Logging / DI Abstractions 8.0.2"]
    MY --> Http["Http.Abstractions 2.3.9"]
    PG --> Npgsql["Npgsql 10.0.1"]
    PG --> Config
    Npgsql --> Log10["Logging / DI Abstractions 10.0.0"]
    MO --> MongoDriver["MongoDB.Driver / Bson 3.5.2"]
    MongoDriver --> Sharp["SharpCompress 0.30.1"]
    MongoDriver --> Snappy["Snappier 1.0.0"]
    OR --> OracleDriver["ManagedDataAccess.Core 23.26.0"]
    SF --> SnowDriver["Snowflake.Data 5.3.0"]
    SF --> Aws["AWSSDK.Core 4.0.3.8"]
    SnowDriver --> S3["AWSSDK.S3 4.0.4"]
    S3 --> Aws
    SnowDriver --> Log9["Logging / DI 9.0.5"]
    SnowDriver --> Unix["Mono.Unix 7.1.0-final.1.21458.1"]
~~~

The six outgoing application edges are alternatives, not an instruction to install every provider. The diagram highlights relevant paths. The complete, versioned direct/transitive graphs, compile assets, runtime assets, and RID assets for **each local and published consumer** are in [dependency-map.json][e-dependencies]; original assets snapshots retain the direct package declarations.

Configuration abstractions currently resolve through Binder despite their suppressed direct reference. That is a fragile public dependency declaration, but **no missing configuration assembly was reproduced**. DI is a demonstrated failure: MSSQL resolves package 9.0.4, MySQL 8.0.2, MongoDB and Oracle none, and Snowflake 9.0.5, while Core's public signatures require assembly major 10. PostgreSQL obtains DI Abstractions 10.0.0 through Npgsql and passes.

NuGet's [PackageReference asset-flow rules](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files) explain the suppressed dependency. The direct fix belongs in metadata for assemblies actually used by public/runtime contracts. A temporary experiment changed **only the copied Core nuspec**, adding DI Abstractions 10.0.1, then used another isolated cache/feed. All five affected provider probes compiled and loaded their public signatures; the three existing registration APIs also ran. This was not a production change, and Oracle/Snowflake still have no registration extension. [Experiment][e-experiment].

Windows SqlClient native assets resolved automatically, and actual SQL Server operations succeeded without a direct SqlClient reference. Other client OS/RID combinations were inspected in metadata but not executed. Snowflake's Mono.Unix assets were present; stage-file/native execution remains unverified without an account. Do not treat a Windows constructor smoke check as exhaustive cross-platform asset validation.

### Fresh source versus published packages

| Published package | Embedded repository commit | Comparison with fresh source package |
|---|---|---|
| Core 1.3.7, MSSQL 1.3.7, PostgreSql 1.3.4 | 4544886ef75204e54ce564f3e7d46a32d6be3fb7 | Dependency groups match; DLL/readme hashes differ |
| MySql 1.3.4 | 41273661c05a6a96be19e7cf8657254cfd6720cb | Published Core minimum 1.3.6 versus fresh 1.3.7; two AddJSONParams signatures changed |
| MongoDB 1.3.3 | c0ff396979192749ebeda4c2716ab50ca8b5cc01 | Published Core minimum 1.3.6 versus fresh 1.3.7; DLL/readme hashes differ |

The initial published comparison feed contained Core 1.3.7, so MySQL/MongoDB's minimum 1.3.6 ranges selected 1.3.7 with NU1603 warnings. That is a constrained-feed comparison, not natural public-feed resolution. A [supplemental isolated test][e-minimum] downloaded historical Core 1.3.6 and offered both versions in another feed/cache: both consumers selected **Core 1.3.6**, restored/compiled/constructed successfully, and still failed clean registration/public-signature DI closure. Historical Core 1.3.6 declares commit 3b052143feb7f8d3df82d87054c6b04e6a7bae24 and targets net10.0; it is not the reviewed source baseline.

Local, published, and supplemental caches were separate, so identical provider versions did not substitute for one another. Previous commits are historical package evidence. [Comparison][e-comparison], [public API differences][e-api-diff].

Reflection found no public signature differences for MSSQL, PostgreSQL, or MongoDB, including the inspected Core API. That does not prove identical implementation behavior. MySQL's released five-argument AddJSONParams methods, whose final arguments are optional, have become three-argument methods in the candidate source at the same package version. Existing published binaries have not been replaced by this review; the compatibility risk applies to the next candidate distribution.

An additional [binary metadata check][e-historical-api] confirms the optional operationLabel/paramsLabel defaults and the DLL hash. The [source at MySQL's declared package commit][e-historical-source] already has three-argument methods, unlike the released DLL. **That provenance discrepancy is confirmed; its cause is unknown.** A stale build or uncommitted build input is a hypothesis. Use the actual published binary as the compatibility baseline rather than trusting the repository SHA alone.

## 3. Prioritized findings

P1 means an important consumption/correctness failure; P2 means a significant reliability, compatibility, security, or usability problem; P3 means a bounded improvement. No P0 was established. Effort: S approximately one focused day, M several days, L a larger API/test change; these are planning estimates.

Every row below is confirmed by source plus the linked artifact, except a future risk explicitly marked conditional. Source links refer to the reviewed commit. A passing probe process can contain recorded failing scenarios; the result values, not exit code alone, determine the verdict.

| ID / severity | Evidence and source | Consumer impact / root cause | Recommended fix | Effort / compatibility |
|---|---|---|---|---|
| F01 **P1** public DI dependency suppressed | [Core csproj:26][s-core-package]; [clean matrix][e-consumers], per-provider registration/run logs; [metadata-only fix][e-experiment] | Registration fails with CS1705 for MSSQL/MySQL and missing DI namespace for MongoDB. Core signature loading fails for five providers. Public dependency marked PrivateAssets=all. | Publish DI Abstractions 10.0.1 through Core; audit every public reference and rebuild dependent packages. Keep compile/runtime driver assets transitive. | S; patch, low API risk, revalidate resulting graph |
| F02 **P1** MongoDB default find throws | [Mongo source:148][s-mongo-options]; [live results][e-mongo]; [projection conversion probe][e-mongo-registration] | Assigning nullable one-type-argument ProjectionDefinition to the driver's two-type-argument projection invokes an implicit adapter that rejects null. Ordinary find-all cannot execute. | Leave Projection unset when absent; assign/convert only a non-null definition. Cover typed and untyped defaults. | S; patch, low risk |
| F03 **P1** incompatible typed overloads/conversion | [BaseDatabaseSource:337][s-overload], [typed return:128][s-typed-return], [Mongo typed return:265][s-mongo-typed], [Mongo conversion:486][s-mongo-conversion]; [SQL live results][e-sql], [Mongo results][e-mongo], [repro][e-repro] | Generic and non-generic parameter bases are siblings. Two-type-argument overload casts to the wrong sibling before execution; typed source-interface path casts the input to its typed sibling after reading. Mongo repeats the invalid return cast and its converter drops typed filters through failed as-casts. An unfiltered live read through that broken typed path was not demonstrated. | Use a common internal command contract and construct a valid typed result; adapt each existing overload without impossible casts. Preserve typed Mongo filters/projections with the correct model serializer instead of reflection/as-conversion; test all rows survive conversion. | M; patch, medium behavior risk |
| F04 **P1** empty typed result retains previous rows | [BaseDatabaseSource:455][s-empty-result]; [four SQL fixtures][e-oracle], [stale value 99 repro][e-repro] | Result is assigned only for one or multiple rows, never zero. Reusing parameters returns rows from a different query; first empty result remains null. | Assign an empty collection on every completed empty typed query, including transaction/Mongo paths. Separate requests and results in future API. | S; patch; empty-null to empty-list behavior change must be documented |
| F05 **P1** output/return values lost | [parameter cloning][s-sql-parameter], [non-query completion][s-nonquery]; [SQL Server][e-sql], [PostgreSQL][e-pg], [MySQL][e-mysql], [Oracle][e-oracle], [fake output][e-repro] | Direction reaches the driver, but created DbParameter values are never copied back. SQL Server procedure sets output 42 and return 9; public values remain 0. PostgreSQL INOUT/MySQL OUT and Oracle fixed-size output also remain 0. PostgreSQL CALL's result row independently shows server output 42. | Retain parameter associations; copy outputs after execution/reader closure on all paths. Add an explicit immutable outputs result in new API. | M; patch, low signature risk |
| F06 **P1** factory returns expired scoped objects | [factory:53][s-factory-scope]; [disposable custom source repro][e-repro] | Factory creates and disposes a nested scope before returning the resolved service, outside the caller's scope. Custom scoped source is already disposed. Current relational sources are not IDisposable, so this is not evidence they themselves were disposed. | Resolve in the caller's scope using a scoped resolver, or return an explicit owning lease if the factory owns a scope. Preserve scoped dependencies until work completes. | M; patch candidate, medium lifetime behavior risk |
| F07 **P1** relational policy silently ignored | [generic constructor:633][s-policy-hidden]; [policy calls=0][e-repro], build CS0108 | Generic base hides the policy member and calls base(connectionString), dropping the policy read by execution methods. MSSQL/MySQL/PostgreSQL advertise resilience that does not run. | Pass the policy through the base constructor and remove the hidden member **together with F08 safeguards**, rather than activating unsafe retries alone. | M; coordinated patch; high behavior risk if shipped alone |
| F08 **P1** retry semantics unsafe/inconsistent | [policy:32][s-retry], [connection outside attempts][s-nonquery]; [repro][e-repro] | ArgumentException retried three times; a failed command is followed by reopening an already-open connection, with three opens/one execution. Simulated acknowledged-loss write action repeats three times. Mongo policy is active; generic relational policy is currently ignored. Real duplicated DB writes were not demonstrated. | Classify transient failures per provider, own a fresh connection/command per attempt, validate retry limits, add bounded backoff, and require explicit idempotency for writes. Never retry ambiguous commits/transaction commands. Document cooperative timeout. | L; safe defaults/bug fixes patch candidates; explicit options minor; behavior changes need migration |
| F09 **P1** Oracle default parameter size fails | [OracleSource:34][s-oracle-size], [neutral Size default][s-neutral-parameter]; [stack/workaround][e-oracle] | Default Size=-1 is unconditionally assigned to OracleParameter.Size, throwing before a parameterized insert or output block. Explicit integer size 0/string size 100 allows execution. | Treat unspecified size as provider-specific omission; validate sizes, require capacity where necessary for variable-width outputs. Preserve SQL Server max-length semantics explicitly. | S; patch, low risk; provider-specific tests essential |
| F10 **P2** cancellation absent from ordinary operations | [IDataSource][s-source-interface]; [zero token overloads][e-repro] | Ordinary reader/scalar/write/health callers cannot forward RequestAborted. Internal async calls receive CancellationToken.None without an effective policy. Transactions do accept tokens. | Add token overloads on concrete classes/extensions or a new interface; flow through routing, OpenAsync, execute, reading and cursor disposal. Do not add abstract members to a public interface in a patch. | M; additive minor; interface replacement major |
| F11 **P2** SQL defaults conflict with examples | [defaults:15][s-command-defaults]; [live SELECT failure][e-sql], [default repro][e-repro] | Default StoredProcedure treats SELECT as a procedure name. Timeout=0 overrides driver finite defaults; SQL Server interprets zero as unlimited. | Fix existing examples with explicit Text and finite timeout. Give new query API Text/30-second defaults; retain legacy defaults until a major migration. | S/M; docs patch, new facade minor, old default change major |
| F12 **P2** mutable name-keyed registration | [factory:24/31/127][s-factory-map]; [collision/mutation repro][e-repro] | Two parameter types with the same simple name collide; callers can clear the singleton dictionary. Convention lookup also writes it without synchronization. The race is a source-based concurrency risk, not a measured race failure. | Key by Type/open generic family, freeze registrations after building the host, use a synchronized cache if fallback remains, expose read-only snapshots through a new contract. | M; additive minor; changing Dictionary return type major |
| F13 **P2** provider registration inconsistent | [Mongo extensions:48][s-mongo-registration], [MySQL extensions:50][s-mysql-registration]; [hosted probes][e-example-checks] | SQL factory works without Use via assembly-name convention; MongoDBParams maps to MongoDB, not MongoDBSource, so needs Use. MySQL IApplicationBuilder overload searches ServerFeatures instead of ApplicationServices and throws despite registration. Add-provider alone does not install Core resilience defaults. | Register explicit mappings in Add-provider, make Use an idempotent compatibility shim, use ApplicationServices, validate missing connection strings early, define whether resilience is opted in. | M; patch registration fix, options minor; preserve old initialization |
| F14 **P2** Mongo client/cursor ownership unclear | [Mongo ctor:20][s-mongo-client], [two scoped factories][s-mongo-registration], [cursors:154/176][s-mongo-options]; [aliases differ][e-mongo-registration] | Concrete and interface resolutions create different scoped sources/clients. Source never disposes its owned MongoClient and does not explicitly dispose cursors. Long-running resource-growth magnitude was not measured. | Share the driver client at appropriate host lifetime, alias one source instance, distinguish owned/borrowed client, and dispose cursors on success/failure/cancellation. | M; patch/minor depending constructor additions; medium ownership risk |
| F15 **P2** audited vulnerable transitive packages | [Mongo csproj:30][s-mongo-package]; [NuGet audit][e-audit], [upstream analysis](#dependency-security-and-reachability) | Driver 3.5.2 resolves SharpCompress 0.30.1 and Snappier 1.0.0 with moderate/high advisories. Normal driver compressor paths inspected do not use the identified vulnerable APIs. | Update to a supported, advisory-free compatible driver graph, rerun API and integration checks, and enforce a documented audit gate. Avoid arbitrary transitive pins as the only repair. | M; patch/minor if public compatibility retained; driver API exposure increases risk |
| F16 **P2** candidate removes released MySQL signatures / provenance differs | [DbParameterExtensions:42][s-mysql-json]; [public API diff][e-api-diff], [binary parameter metadata][e-historical-api], [declared-commit source][e-historical-source] | Optional arguments are still part of binary signatures. Released callers reference five-argument methods; fresh 1.3.4 lacks them. Published source SHA does not reproduce that public signature, for an unknown reason. Reusing versions also obscures changed artifacts. | Restore forwarding legacy overloads, allocate new versions, compare against the actual published binary, and build/pack from the same clean inputs. Remove only in a later major. | S; restorative patch; removal major |
| F17 **P2** transaction setup leaks command on failure | [command creation:197][s-transaction-command]; [cleanup repro][e-repro] | GetCommand creates a command; ConfigureTransactionCommand throws during parameter creation before the caller's await-using acquires it. Connection/transaction dispose, command does not. | Dispose the partially configured command in a catch, then rethrow; verify all parameter/timeout/type setter failure paths. | S; patch, low risk |
| F18 **P2** mapping contracts inconsistent | [dictionary catch:53][s-dictionary-map], [mapper:502/541][s-mapping], [raw columns:264][s-raw-map]; [repro][e-repro], [duplicate columns][e-sql] | Bad integer dictionary mapping silently yields 0; DBNull into non-nullable property preserves initializer 99; indexer causes type initializer failure; DateTime-to-DateOnly conversion fails; duplicate raw column names overwrite earlier values. | Exclude indexers, add explicit modern date/time conversions, document null behavior, offer strict conversion/duplicate-column handling, and consolidate mapping behavior behind compatibility adapters. | M; additive support patch/minor; legacy failure/null semantics changes major or opt-in |
| F19 **P2** mapping exception includes row data | [conversion error:416][s-mapping-error]; [SECRET-SENTINEL fixture][e-sql] | Library adds raw field value to its exception message, exposing it to common exception logging. No password leak was demonstrated. | Remove added raw values; retain property/type and cause category, document/redact driver/inner exception details deliberately, keep sensitive diagnostics opt-in. | S; patch, low risk |
| F20 **P2** release gate omits package consumption | [publish workflow:38][s-release], [CI][s-ci]; [warnings/API/package failures][e-build] | CI tests projects; release job builds/packs/pushes without running tests or clean consumers. Floating 10.0.x and unchanged package versions reduce reproducibility; skip-duplicate can skip a changed candidate. | Gate release on tests, isolated package consumers, docs compilation, API baseline validation, selected advisories/conflicts; pin SDK policy and record artifact hashes/provenance. Centralize genuinely shared settings. | M; tooling patch, no public API risk |
| F21 **P2** documentation fails real consumption | [MSSQL README:82][s-sql-doc], [PG README:8/48/59][s-pg-doc], [Core README:144][s-core-doc]; [41-fence audit][e-all-docs], [focused checks][e-example-checks] | Wrong Add/Use names, package ID, driver parameter lists and record constructor; unsupported retry/circuit-breaker members and TimeSpan timeout; unnecessary direct driver/GitHub-feed instructions. Some snippets compile but fail at execution. | Generate/test examples against packed packages; distinguish host prerequisites, current capabilities and proposed APIs. Install one chosen provider from NuGet.org. | M; docs patch, low risk |
| F22 **P3** local providers lack release/support contract | [Oracle][s-oracle-package], [Snowflake][s-snowflake-package], [workflow][s-release]; [inventory][e-inventory], [API inventory][e-public-api] | No registration extensions or managed transaction providers; local packages lack readme/license/repository URL and meaningful versioning. Snowflake integration blocked without account. | Define support scope and metadata; add registration and driver-supported transactions as additive work; require account-backed checks before promising Snowflake readiness. | M/L; metadata patch; capabilities minor; publication requires separate authorization |
| F23 **P3** secondary Core source semantics incomplete | [JsonFileSource:23/30][s-file-source], [StaticCodeSource][s-static-source]; [owned-file and static typed repro][e-repro] | File writer requires an existing path; writing one UTF-8 character reports 1 while writing 2 bytes. Static typed overload rejects valid typed parameters. File errors retain inner exceptions but wrap them generically. | Specify create/overwrite semantics, correct byte-count contract, fix typed adapters, preserve useful exception types. | S/M; byte/typed fixes patch candidates; new create behavior needs an explicit migration/option |

### Ownership, async, transactions, and concurrency

Ordinary relational success/failure paths surround connections, commands, and readers with using. The focused mapping-failure probe confirms connection/command disposal and reader closure. This is evidence against a blanket claim that ordinary SQL reads leak connections. Synchronous disposal inside async methods is valid; no measured blocking-disposal problem justifies rewriting it solely for style.

Managed transactions use await using for connection/transaction and commands/readers. The callback's executor uses a SemaphoreSlim, links cancellation, drains pending work, and prevents use after completion. Live SQL Server/PostgreSQL/MySQL checks confirm commit, rollback, cancellation rollback, and escaped executor rejection. Existing fake tests also cover rollback failure, commit failure with unknown outcome, and serialization. Preserve these behaviors. F17 is a narrower exception during command construction, outside the protected lifetime.

GetConnection creates a new **caller-owned** connection. SQL Server's live ownership probe opens one, executes a separate source operation, and observes it still open. Ordinary operations do not enlist it. There is no public attach-existing-connection/transaction contract. A future borrowed connection option must explicitly state ownership, avoid closing caller resources, validate connection/transaction pairing, and forbid implicit retries. External/nested/distributed transaction capabilities should be added only for a demonstrated requirement and supported driver.

Parameters and their Value containers are mutable. Concurrently executing the **same parameter object** can race on results/output values; no claim of thread-safe parameter reuse is warranted. Separate requests per operation are the current safe contract. The factory dictionary is a different shared-state problem, reproduced by collision/mutation and assessed for unsynchronized access.

Cancellation was checked both after a write (verifying rollback) and during a ten-second server delay inside a transaction. SQL Server/PostgreSQL/MySQL interrupted in about 229/250/242 ms and remained healthy. SQL Server surfaced SqlException with an explicit user-cancellation message; the other two surfaced OperationCanceledException. This is a bounded check, not sustained cancellation stress testing. Ordinary methods' inability to accept a token is a public API finding, not a claim that drivers cannot cancel. Preserve useful driver errors and document their cancellation behavior rather than automatically treating every exception as a wrapper defect.

The use of short-lived logical connections is consistent with [SqlClient pooling](https://learn.microsoft.com/en-us/sql/connect/ado-net/sql-server-connection-pooling?view=sql-server-ver17). Keep per-operation ownership; do not replace it with one globally shared mutable connection. For PostgreSQL, a shared [NpgsqlDataSource](https://www.npgsql.org/doc/basic-usage.html) can own pooling/configuration while individual operations retain their own connections. Adopt it only if adding configuration/driver features that need it; no pool throughput defect was measured here.

DI lifetime changes should follow [Microsoft's disposal guidance](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines): the owning scope must outlive the resolved service. MongoDB recommends long-lived client reuse and documents client disposal in its [current client guide](https://www.mongodb.com/docs/drivers/csharp/current/connect/mongoclient/). Disposing one client is not a reason to indiscriminately tear down a shared cluster/client.

### Parameters, capabilities, and driver exposure

[Exported member inventory][e-driver-api] contains **zero driver-specific declared public member signatures for the five relational providers**, and twelve for MongoDB. The relational providers expose DbConnection/DbCommand, neutral DataAccessParameter, System.Data.CommandType, and Core contracts. A Core namespace import is not a direct Core package installation. Underlying connection instances are driver objects, but consumers need not name those types for ordinary operations.

MongoDB's parameter contracts expose BsonDocument and driver Filter/Projection/Sort/Update/PipelineDefinition types. These resolve automatically through its package dependency, so installation succeeds; using the ordinary document API still requires driver concepts. Retain them for advanced document features, and add a typed document facade rather than forcing MongoDB into SQL command semantics.

Typed MongoDBParams<T> exposes typed documents/updates, but ordinary non-query methods require the non-generic parameter base, so that public typed request has no matching typed-write contract. This is an API inspection finding; no typed-write runtime success is claimed. Factory CreateParams<T> is also a public stub that always throws NotImplementedException ([source:87][s-factory-params]); remove it from quick starts, implement it, or deprecate it through the compatibility process.

Neutral direction mapping reaches driver parameters; the missing step is output propagation. Precision, scale, SQL Server table-valued parameter type names, bulk copy, PostgreSQL arrays/ranges/composites, Oracle array binding/ref cursors, and Snowflake stage/query settings are not adequately represented by the basic five-field neutral parameter. These are **capability gaps assessed from the API**, not failures reproduced for every advanced type. Provide explicit provider-specific configuration/escape hatches instead of expanding a universal enum to hide every driver feature.

The injection-like string used in four SQL fixtures was bound as a value and read back unchanged; no injected DROP executed. This validates the tested parameter paths. Raw Query strings are still accepted, so caller-built unsafe SQL is not made safe by this wrapper. Dynamic identifiers require application validation/quoting appropriate to the provider.

### Dependency security and reachability

The October 5 audit reports only the following affected dependencies in this solution graph:

| Dependency | Resolved | Advisory / affected API | Assessment |
|---|---:|---|---|
| SharpCompress | 0.30.1 | [GHSA-6c8g-7p36-r338](https://github.com/advisories/GHSA-6c8g-7p36-r338): archive directory extraction traversal; fixed 0.48.0 | Mongo driver's inspected compressor uses ZlibStream, not directory archive extraction |
| Snappier | 1.0.0 | [GHSA-pggp-6c3x-2xmx](https://github.com/advisories/GHSA-pggp-6c3x-2xmx): framed SnappyStream decompression loop; fixed 1.3.1 | Driver's inspected compressor uses static raw Snappy calls, not SnappyStream |

The upstream version-matched [SnappyCompressor](https://raw.githubusercontent.com/mongodb/mongo-csharp-driver/v3.5.2/src/MongoDB.Driver/Core/Compression/SnappyCompressor.cs) and [ZlibCompressor](https://raw.githubusercontent.com/mongodb/mongo-csharp-driver/v3.5.2/src/MongoDB.Driver/Core/Compression/ZlibCompressor.cs) support that narrower assessment. **A reachable exploit through normal wrapper calls was not demonstrated.** Consumers also receive these libraries transitively and may use them elsewhere; an advisory-free supported graph is still appropriate dependency hygiene. [Audit output][e-audit].

MongoDB's [3.8.1 release notes](https://github.com/mongodb/mongo-csharp-driver/releases/tag/v3.8.1) and [release history](https://github.com/mongodb/mongo-csharp-driver/releases) provide upgrade/security context. As inspected, the latest driver release was 3.12.0. This review did not validate an upgrade candidate; choose a supported version, inspect its resulting graph, and rerun public API/serializer/integration tests before selecting it. Do not assume a direct SharpCompress override across many releases is compatible.

Snowflake's warning concerns **DI assembly major 9 versus 10**, not a demonstrated AWS major-version conflict. Its driver graph uses AWS SDK v4, consistent with the direct v4 Core reference. The wrapper has no observed AWSSDK.Core usage, so removing its redundant direct pin is conditional on a fresh graph/API/runtime check. The [official connector](https://github.com/snowflakedb/snowflake-connector-net) documents its supported frameworks, pooling, and advanced capabilities.

Tracked configuration inspection found integrated security/placeholders/empty connection strings, and a NuGet template with token placeholders, not a verified embedded credential. This was a configuration/source audit, not a complete secret-history scan. Health methods catch failures and return false, making authentication/network/configuration causes indistinguishable. Add structured diagnostic outcomes without connection-string/row-value logging; BCL ActivitySource can cover operation duration, attempts and status without adding an observability dependency.

### Measured performance

The [reproduction probe][e-repro] uses warmed typed mapping over a one-column BCL DataTable reader, five operations per row count, with no server/network. The retained [measurement baseline][e-measurement] used below measures total elapsed time and process allocation delta; later reruns vary:

| Rows per operation | Operations | Total elapsed | Total allocated |
|---:|---:|---:|---:|
| 1 | 5 | 0.1278 ms | 7,840 bytes |
| 1,000 | 5 | 8.9461 ms | 329,720 bytes |
| 10,000 | 5 | 40.3403 ms | 3,719,000 bytes |

These are one run's local measurements, not throughput benchmarks or a comparison against another mapper. Timings vary across runs and include reader/materialization work. Source and results establish buffering and allocation growth; they do not establish that reflection, lookup, or this wrapper dominates application latency. Setters are already cached. Do not add Dapper or another mapper based on these numbers alone.

For applications with large result sets, evaluate an additive streaming API against the existing path: measure first-row latency, peak memory, allocations and early-break/cancellation cleanup on representative payloads. Cache schema accessors further only if a focused profile shows repeated schema work is material. Npgsql's [performance guide](https://www.npgsql.org/doc/performance.html) and MySqlConnector's [async guidance](https://mysqlconnector.net/tutorials/best-practices/) support driver-aware testing, not a universal micro-optimization.

## 4. Consumer API and concrete examples

### Installation and currently usable paths

Choose **one** package for the selected provider:

~~~powershell
dotnet add package DataAccessProvider.MSSQL --version 1.3.7
# Alternatives:
dotnet add package DataAccessProvider.MySql --version 1.3.4
dotnet add package DataAccessProvider.PostgreSql --version 1.3.4
dotnet add package DataAccessProvider.MongoDB --version 1.3.3
~~~

No explicit Core or driver package is required. Oracle/Snowflake examples currently require the freshly packed local feed under their evaluated 1.0.0 versions; these are not verified public installation commands. Keep net10.0. A host's own framework/package requirements are separate from DataAccessProvider dependencies.

**Before: existing documentation problems.**

~~~csharp
// MSSQL README currently names a different provider's Use method.
services.AddDataAccessProviderMSSQL(configuration);
serviceProvider.UseDataAccessProviderMySQL();

// A SELECT without CommandType uses StoredProcedure.
var p = new MSSQLSourceParams { Query = "SELECT Id FROM Users" };

// PostgreSQL README assigns a driver list to a neutral list property.
var q = new PostgresSourceParams {
    Parameters = new List<NpgsqlParameter> { new("@id", 1) }
};
~~~

**After: current MSSQL API, driver-free constructor path.** This pattern compiles in the clean consumer and its operations were exercised by the separate SQL fixture. It deliberately sets the current command defaults and allocates a new request per query.

~~~csharp
using System.Data;
using DataAccessProvider.Core.Types;
using DataAccessProvider.MSSQL;

// Obtain the connection string from application configuration/secrets.
var source = new MSSQLSource(connectionString);
var query = new MSSQLSourceParams<User> {
    Query = "SELECT Id, Name FROM Users WHERE Id = @id",
    CommandType = CommandType.Text,
    Timeout = 30,
    Parameters = new() {
        new() { ParameterName = "@id", DbType = DataAccessDbType.Int32, Value = 1 }
    }
};
var users = (await source.ExecuteReaderAsync<User>(query)).Value;

var count = (await source.ExecuteScalarAsync(new MSSQLSourceParams {
    Query = "SELECT COUNT(*) FROM Users",
    CommandType = CommandType.Text, Timeout = 30
})).Value;

var write = new MSSQLSourceParams {
    Query = "UPDATE Users SET Name=@name WHERE Id=@id",
    CommandType = CommandType.Text, Timeout = 30
};
write.AddParameter("@name", DataAccessDbType.String, "Ada", size: 100);
write.AddParameter("@id", DataAccessDbType.Int32, 1);
int affected = (await source.ExecuteNonQueryAsync(write)).AffectedRows;

public sealed class User {
    public int Id { get; set; }
    public string? Name { get; set; }
}
~~~

MySQL substitutes MySQLSource/MySQLSourceParams; PostgreSQL substitutes PostgresSource/PostgresSourceParams, uses the PostgreSql package ID, and can populate the neutral list directly. Oracle substitutes OracleSource/OracleSourceParams, uses :id/:name placeholders and explicitly valid sizes until F09 is fixed. Snowflake uses SnowflakeSource/SnowflakeSourceParams and its driver's supported binding syntax; query execution is conditional on account-backed verification.

**Current host registration, corrected names.** This example assumes a host that supplies the full DI implementation, such as ASP.NET Core. It does not repair F01 in a plain application.

~~~csharp
using Microsoft.Extensions.DependencyInjection;
using DataAccessProvider.MSSQL;

services.AddDataAccessProviderMSSQL(connectionString);
// Build the host/service provider after registration.
using var root = services.BuildServiceProvider();
root.UseDataAccessProviderMSSQL(); // Explicit, currently supported initialization.
using var scope = root.CreateScope();
var source = scope.ServiceProvider.GetRequiredService<MSSQLSource>();
~~~

Correct alternative names are AddDataAccessProviderMySql / UseDataAccessProviderMySql, AddDataAccessProviderPostgres / UseDataAccessProviderPostgres, and AddDataAccessProviderMongoDB / UseDataAccessProviderMongoDB. MongoDB currently requires Use for factory routing. SQL factory convention fallback works in the tested host but should not be the advertised initialization contract. Oracle/Snowflake currently require manual source registration if using the factory; direct construction avoids that extra setup. [Host probe source][e-host-source], [corrected PostgreSQL query/transaction compilation][e-corrected].

**Current managed transaction and disposal.**

~~~csharp
using System.Data;
using DataAccessProvider.MSSQL;

var source = new MSSQLSource(connectionString);
await source.ExecuteInTransactionAsync(async (tx, ct) => {
    var debit = new MSSQLSourceParams {
        Query = "UPDATE Accounts SET Balance=Balance-@amount WHERE Id=@id",
        CommandType = CommandType.Text, Timeout = 30
    };
    debit.AddParameter("@amount", DataAccessProvider.Core.Types.DataAccessDbType.Decimal, 100m);
    debit.AddParameter("@id", DataAccessProvider.Core.Types.DataAccessDbType.Int32, 1);
    await tx.ExecuteNonQueryAsync(debit, ct);
    // Execute the corresponding credit through tx before returning.
}, IsolationLevel.ReadCommitted, cancellationToken);
// Callback success commits; failure/cancellation rolls back.
// The source owns the callback's connection/transaction; do not retain tx.

// Advanced caller-owned connection: it does not enlist source operations.
await using var callerOwned = source.GetConnection();
await callerOwned.OpenAsync(cancellationToken);
// Use callerOwned's commands directly when intentionally managing this lifetime.
~~~

The source itself is not a disposable connection. Its ordinary operations create/dispose their own resources. Transaction callback tokens provide cancellation today; ordinary source methods have no corresponding token overload. An input-only stored procedure can use CommandType.StoredProcedure today. Public output/return values are unreliable until F05 is fixed; do not recommend installing SqlClient as a solution to that wrapper defect.

**Current MongoDB workaround** still uses transitive driver concepts; it is not the desired final ordinary API:

~~~csharp
using DataAccessProvider.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

var source = new MongoDBSource(connectionString);
var find = new MongoDBParams {
    CollectionName = "users",
    Projection = Builders<BsonDocument>.Projection.Include("_id").Include("Name"),
    Sort = Builders<BsonDocument>.Sort.Ascending("_id")
};
var rows = (await source.ExecuteReaderAsync(find)).Value;
// Typed Mongo paths remain defective; the explicit projection avoids F02 only.
~~~

### Proposed API: additive minor release, not implemented

Preserve existing providers and extension names. Make each Add-provider call register a validated connection configuration, explicit type mapping, one source instance, and its optional policies without a required Use call. Keep Use as an idempotent legacy shim.

Introduce independent query/result contracts rather than replacing the sibling parameter hierarchies in place. Provider markers let several databases coexist in one host without ambiguous generic registrations:

~~~csharp
// PROPOSAL: exported by Core; provider marker exported by its provider package.
public interface IDatabaseClient<TProvider> {
    Task<IReadOnlyList<T>> QueryAsync<T>(
        DatabaseCommand command, CancellationToken cancellationToken = default);
    Task<T?> ScalarAsync<T>(
        DatabaseCommand command, CancellationToken cancellationToken = default);
    Task<CommandResult> ExecuteAsync(
        DatabaseCommand command, CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(
        Func<IDatabaseSession, CancellationToken, Task> action,
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default);
}

// PROPOSAL: immutable request; Text and 30 seconds by default.
// Parameter definitions include optional size/precision/scale and neutral direction.
// CommandResult contains AffectedRows, Outputs, and optional ReturnValue.
// Results never overwrite the request; Query always returns a collection.
~~~

For broad compatibility, initially support writable POCO mapping and explicit mappers; do not promise record-constructor mapping until implemented/tested. A concrete provider client constructor should support plain one-package applications without DI. Host registration exposes IDatabaseClient<SqlServer>, IDatabaseClient<PostgreSql>, etc. Host-owned clients are disposed by their host if they own driver resources; constructor-created owning clients expose IAsyncDisposable. Borrowed resources require explicit ownership options.

**Proposed installation stays one provider package** at its new release version; driver dependencies remain normal transitive dependencies.

~~~csharp
// PROPOSAL, in a host that already supplies DI:
services.AddDataAccessProviderMSSQL(connectionString);
var db = scope.ServiceProvider.GetRequiredService<IDatabaseClient<SqlServer>>();

var users = await db.QueryAsync<User>(
    DatabaseCommand.Text("SELECT Id, Name FROM Users WHERE Id=@id")
        .WithParameter("@id", DataAccessDbType.Int32, 1),
    cancellationToken);

int count = await db.ScalarAsync<int>(
    DatabaseCommand.Text("SELECT COUNT(*) FROM Users"), cancellationToken);

var result = await db.ExecuteAsync(
    DatabaseCommand.Text("UPDATE Users SET Name=@name WHERE Id=@id")
        .WithParameter("@name", DataAccessDbType.String, "Ada", size: 100)
        .WithParameter("@id", DataAccessDbType.Int32, 1),
    cancellationToken);

var procedure = await db.ExecuteAsync(
    DatabaseCommand.StoredProcedure("review_output")
        .WithOutput("@out", DataAccessDbType.Int32)
        .WithReturnValue("@return", DataAccessDbType.Int32),
    cancellationToken);
int output = (int)procedure.Outputs["@out"]!;
int returnValue = (int)procedure.ReturnValue!;

await db.ExecuteInTransactionAsync(async (tx, ct) => {
    await tx.ExecuteAsync(debitCommand, ct);
    await tx.ExecuteAsync(creditCommand, ct);
}, IsolationLevel.ReadCommitted, cancellationToken);
// Use tx for every atomic operation; no automatic retry of this transaction.
~~~

**Proposed document facade** should use document types and typed filters/expressions for common work, with the driver-native aggregation/update definitions retained on an explicit advanced surface:

~~~csharp
// PROPOSAL, after one AddDataAccessProviderMongoDB registration:
var documents = scope.ServiceProvider.GetRequiredService<IDocumentClient>();
await documents.InsertAsync("users", new User { Id=1, Name="Ada" }, cancellationToken);
var users = await documents.FindAsync<User>(
    "users", user => user.Id == 1, cancellationToken);
~~~

Do not promise LINQ or expression translation beyond the tested supported subset. For native capabilities, offer explicit SqlServerAdvanced/PostgresAdvanced/OracleAdvanced/SnowflakeAdvanced/DocumentAdvanced extensions or command configuration callbacks. Drivers may appear there; consumers still do not manually install them.

### Dependencies justified by these proposals

- **DI Abstractions 10.0.1:** required now by existing public/runtime contracts; metadata-only experiment demonstrates its value.
- **Full DI implementation / hosting:** application-host concern when using a host. If promising that a bare console can call BuildServiceProvider with only the provider reference, deliberately declare a compatible full DI implementation and test that promise. The metadata experiment proves abstractions/registration, not a universal standalone host builder.
- **Configuration.Binder:** existing Core configuration binding uses it. Public configuration abstractions should be accurately declared even if Binder supplies them today.
- **Driver packages:** essential implementation/runtime dependencies; preserve their normal transitive flow.
- **No new resilience/ORM/mapper/logging package is justified by this review.** Repair ownership/classification/cancellation first. Existing driver features and BCL diagnostics cover the demonstrated requirements. A future dependency requires a measurable capability/maintenance benefit and compatible API/size/security evidence.
- **AWSSDK.Core direct pin and Http.Abstractions:** assess removing or isolating redundant/host-only references after closure/API checks. Keep the old MySQL IApplicationBuilder signature through a compatible migration; dropping its support dependency immediately would break that public surface.

## 5. Ordered implementation backlog and migration

These are proposed tasks, not changes performed by this review. Dependencies specify the implementation order; release classification assumes existing signatures are retained.

| Order / candidate | Depends on | Scope | Measurable acceptance criteria |
|---|---|---|---|
| B1 **patch** package contract and candidate versioning | None | F01, F16; declare actual public dependency requirements, restore MySQL legacy overloads, choose new versions | Seven fresh packs; each provider's single-reference plain net10 consumer restores/builds, loads all public signatures, and invokes each existing registration API without driver/Core references. Published baseline API check has no unintended removals. |
| B2 **patch** deterministic execution correctness | B1 | F02–F05, F09, F17; fix Mongo projection, typed adapters, empty results, output copyback, Oracle sizes, construction cleanup | Every existing typed overload returns the expected 0/1/2 rows. Reused empty request yields zero rows. MSSQL procedure returns 42/9; Oracle block returns 42. Default Oracle inputs succeed. Connection/command/reader/transaction disposal asserted on configuration, execution and mapping failure. |
| B3 **patch/minor** registration and scope ownership | B1 | F06, F12–F14; explicit mappings, host lifetime, Mongo client ownership, MySQL ApplicationServices | Add-provider alone routes each supported family; Use remains safe/idempotent. Two same-name types coexist. Caller scope resolves the same source aliases and keeps disposable dependencies alive until scope disposal. Owned/borrowed client tests and concurrent lookup tests pass. |
| B4 **coordinated patch + additive minor** resilience/cancellation | B2, B3 | F07, F08, F10; policy forwarding, per-attempt resources, provider transient classification, token APIs | Counted policy is invoked on applicable ordinary paths. Permanent failures run once; configured transient failures retry with fresh resources and bounded delay. Non-idempotent operations run once by default. No transaction/unknown commit replay. Pre-cancelled and in-flight cancellation stop work and dispose resources. Cooperative timeout behavior documented/tested. |
| B5 **patch** documentation, security graph, diagnostics and release gating | B1–B4 for claimed examples; dependency upgrade can begin earlier | F15, F19–F23; upgrade/test driver graph, truthful examples/metadata, package CI | All executable doc fences compile; runnable fixtures succeed under supported contracts. Audit gate has no unaccepted advisories. No added raw values in mapper diagnostics. Release consumes its actual packages, runs tests/API validation, records SDK/source/package hashes, and refuses reused changed versions. Oracle/Snowflake support status is explicit. |
| B6 **minor** coherent additive consumer facade | B2–B4 | New command/result/document APIs and provider-specific advanced surfaces | One package supports installation, construction/registration, query/scalar/write, applicable procedure outputs, transaction, cancellation and disposal. 0/1/many results have stable shapes. Legacy adapters remain binary compatible. Multiple providers coexist without ambiguous service resolution. |
| B7 **minor, conditional** mapping/streaming improvements | B2, B6; representative performance requirement | F18 and measured buffering; strict mapping options, supported date conversions, optional streaming | Indexers excluded; nullable/enum/Guid/date/time/culture cases specified and tested. Strict failures identify property/type without values. Benchmark reports payload/schema, warm-up, allocations, first-row latency, peak memory and cleanup; improvement is demonstrated against baseline. |
| B8 **major, only after migration period** retire incompatible contracts | B5–B7 and usage evidence | Replace mutable Dictionary contract, remove obsolete overloads/Use requirements, change legacy defaults/results if desired | At least one stable minor release provides adapters/obsolete guidance. Migration examples compile; API baseline records intentional breaks. Custom source implementers have a documented replacement interface, ownership and cancellation contract. |

An incremental path avoids a simultaneous framework/package/API rewrite: first patch the dependency and result defects; restore binary compatibility; add safe cancellation/explicit registrations; introduce the facade beside existing APIs; migrate samples; then consider major removals. Changing an existing interface or removing optional method parameters is a compatibility change under [Microsoft library guidance](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes).

Use the SDK's [package validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/package-validation/overview) against the released baseline, plus behavioral tests. It directly addresses the MySQL signature removal; it does not replace clean restore/runtime tests. For retry design, [SqlClient's official guidance](https://learn.microsoft.com/en-us/sql/connect/ado-net/configurable-retry-logic-core-apis-sqlclient?view=sql-server-ver17) supports transient classification and explicit transaction boundaries. Do not import an HTTP retry pattern without accounting for database outcome ambiguity.

## 6. Verification results, reproducibility, and open questions

### Independent package checks

Each baseline consumer has one direct provider PackageReference, no ProjectReference, no direct Core/driver reference, and **no shared framework reference**. Ten original cases and two supplementary published-minimum cases were checked. DataAccessProvider.* is mapped exclusively to its selected local/published-binary feed, while other dependencies map to NuGet.org. Local, published, published-minimum, and metadata-experiment feeds/caches are separate. This matters because [NuGet source mapping](https://learn.microsoft.com/en-us/nuget/consume-packages/package-source-mapping) does not reselect a source for an already cached package.

| Provider | Fresh restore / compile / driver construction | Public signature dependency closure | Clean registration | Published package test |
|---|---|---|---|---|
| MSSQL | Passed / passed / passed | **Failed**, missing DI major 10 on signature loading | **Failed**, CS1705; DI package 9.0.4 | Same package results at 1.3.7 |
| MySql | Passed / passed / passed | **Failed**, missing DI major 10 | **Failed**, CS1705; DI package 8.0.2 | Same basic/DI results at 1.3.4 with Core 1.3.7 and natural minimum 1.3.6 |
| PostgreSql | Passed / passed / passed | Passed | Passed compile and runtime registration calls | Same results at 1.3.4 |
| MongoDB | Passed / passed / passed | **Failed**, missing DI major 10 | **Failed**, DI namespace absent | Same basic/DI results at 1.3.3 with Core 1.3.7 and natural minimum 1.3.6 |
| Oracle | Passed / passed / passed | **Failed**, missing DI major 10 | Not applicable: extension absent | Blocked: no accessible public index/binary |
| Snowflake | Passed / passed / passed | **Failed**, missing DI major 10 | Not applicable: extension absent | Blocked: no accessible public index/binary |

Basic run logs deliberately catch public API loading exceptions to record evidence. Their exit 0 means the probe completed, not that dependency closure passed. [Machine-readable check matrix][e-verification] separates these outcomes. The copied-nuspec experiment passes compile/runtime signature checks for all five affected providers, without changing their assemblies. It is a fix-isolation experiment, not a replacement baseline.

Hosted registration probes use Microsoft.AspNetCore.App intentionally and separately. They prove alias/scope/initialization behavior; they do not excuse missing dependencies in the plain package test.

### Database verification

All database checks used new labelled containers, fresh data, dummy fixture credentials and ephemeral **loopback-only** ports. The probe accepts a port, not a configurable destination or inherited connection string. Tested image tags, immutable digests, IDs, and fixture ports are recorded in [database images][e-images] and [fixture summary][e-fixtures]. Owned containers/volumes were removed; [cleanup check][e-cleanup] found zero remaining review containers. Image caches were retained.

| Area | MSSQL | PostgreSQL | MySQL | MongoDB | Oracle | Snowflake |
|---|---|---|---|---|---|---|
| Reachability/health | Passed | Passed | Passed | Passed | Passed after readiness wait | Blocked: no account |
| Parameterized write | Passed | Passed | Passed | InsertMany passed, 2 documents | **Failed** default size; passed explicit valid sizes | Blocked |
| Scalar | Passed | Passed | Passed | Count passed | Passed | Blocked |
| Untyped read | Passed | Passed | Passed | **Failed** default find; explicit projection/sort passed | Passed after insert workaround | Blocked |
| Typed read with typed request, supported single-type-argument path | Passed | Passed | Passed | **Failed** projection/cast/filter paths | Passed after insert workaround | Blocked |
| Other public typed overloads | **Failed** | **Failed** | **Failed** | **Failed** | **Failed** | Shared-code failure assessed; live check blocked |
| Empty typed request reuse | **Failed**, stale rows | **Failed** | **Failed** | Typed path already fails | **Failed** | Blocked |
| Procedure/block outputs | **Failed**, outputs/return stay 0 | **Failed**, INOUT stays 0; CALL row shows 42 | **Failed**, OUT stays 0 | Not applicable to SQL procedure contract | **Failed** default size; fixed-size block executes but output stays 0 | Blocked |
| Managed commit/rollback | Passed | Passed | Passed | Not applicable: no managed transaction API | Not applicable: no managed transaction API | Not applicable: no managed transaction API |
| Transaction cancellation/escaped executor | Passed | Passed | Passed | Not applicable | Not applicable | Not applicable |
| In-flight transaction command cancellation | Passed interruption/health, about 229 ms; SqlException surface | Passed, about 250 ms | Passed, about 242 ms | Not applicable: no transaction API | Not applicable: no transaction API | Not applicable: no transaction API |
| Multiple result sets / duplicate columns / caller-owned connection | Passed / **failed** / passed | Passed / **failed** / passed | Passed / **failed** / passed | Not applicable to this SQL fixture | Not applicable to selected Oracle command fixture; shared Core ownership assessed | Blocked |

Oracle's first attempt reached the listener before FREEPDB1 was registered and failed with ORA-12514. That initial startup artifact is retained separately. A readiness-wait rerun connected successfully and isolated the size failure at OracleParameter.set_Size; a subsequent explicit-size insert/read/block verified the workaround. The initial startup failure is **not** a package failure.

PostgreSQL's initial fixture incorrectly assumed a provider convenience AddParameter overload; its compilation log is retained as a harness correction. The rerun populated the neutral parameter list and succeeded. No provider package build failure is inferred from that harness mistake.

In-flight cancellation was exercised for the three managed SQL providers. Network interruption during an acknowledged write, variable-width output truncation, and all advanced database types were not exercised. The matrix does not infer account-dependent or unsupported capability success from another provider.

### Existing tests and temporary reproductions

| Check | Verdict | Evidence |
|---|---|---|
| Release solution build; seven explicit library packs | Passed, with recorded warnings | build.txt, pack-*.txt, nuspec/inventory |
| Existing 31 Core + 11 MSTest tests | Passed | [tests][e-tests] |
| Factory scoped disposal, collisions, public dictionary mutation | Failed intended lifetime/isolation contracts | [focused results][e-repro] |
| Generic policy forwarding; retries after failure/permanent error | Failed | Same: zero policy calls; reopen failure; three permanent attempts |
| Repeated write handling | Failed safety simulation; live duplicate write **not demonstrated** | Same: three simulated writes after acknowledgement loss |
| Ordinary caller cancellation | Failed API availability; no applicable token overload | Same plus public interface source |
| Cooperative policy timeout | Passed observed cooperative behavior; no hard deadline guarantee | 5 ms cancellation ignored by action; action completed after about 59 ms |
| Ordinary resource cleanup on mapping failure | Passed | Closed reader, disposed command/connection |
| Transaction parameter-setup cleanup | **Failed** command disposal; connection/transaction passed | commandDisposed=false |
| Case-insensitive/numeric/nullable/enum/Guid/bool mapping | Passed tested values | Mapped Id=7, nullable null, Active enum, Guid, true |
| Null non-nullable, DateOnly, indexer, invalid dictionary value | Failed/unsupported contract cases documented | Retains 99; conversion exception; initializer exception; silently 0 |
| Static typed source | Failed | ArgumentException |
| Mapping/allocation measurement | Passed measurement collection; no comparative performance verdict | Three measured row counts |

**Documentation:** all 41 C#-labelled fences across the root/Core/four published-provider READMEs were inventoried. With supplied host/domain scaffolding, **26 compiled, 11 failed, and 4 displayed-output blocks were not applicable**. The all-doc compiler uses four provider packages and a host framework so multi-provider examples have their context; it is deliberately not a clean one-provider proof. Usings/record declarations are relocated into valid surrounding scope while their text/statements are preserved. Original excerpts and compiler diagnostics are retained. [All documentation results][e-all-docs].

Focused one-provider hosted checks compile the corrected PostgreSQL neutral-parameter query/POCO/transaction example and the correctly named provider registration probes. Exact MSSQL/MySQL registration snippets fail; PostgreSQL's driver-list and positional-record snippets fail; root/Core registration snippets fail syntax. Core resilience example fails on TimeSpan-to-int and nonexistent RetryCount/CircuitBreakerThreshold. Compiled default SELECT and Mongo find examples still fail at runtime. Compiler success alone is not consumption readiness.

### Reproduction commands and evidence preservation

[Evidence README][e-evidence-readme] describes script prerequisites, caches, statuses, and each artifact. The main sequence is:

~~~powershell
& ./review/evidence/Run-PackageVerification.ps1
& ./review/evidence/Run-PublishedMinimumCheck.ps1
& ./review/evidence/Inspect-ApiAndDocs.ps1
& ./review/evidence/Run-Reproductions.ps1
& ./review/evidence/Run-ExampleChecks.ps1
& ./review/evidence/Compile-AllDocExamples.ps1
& ./review/evidence/Run-HistoricalApiCheck.ps1
& ./review/evidence/Run-MetadataExperiment.ps1
& ./review/evidence/Run-Integration.ps1
& ./review/evidence/Build-EvidenceIndex.ps1
~~~

The first script logs exact dotnet commands, restores the solution into a separate cache, explicitly packs every library, downloads accessible published binaries, and creates source-mapped consumers. It does not publish. Integration requires Docker and access to the recorded images; Snowflake has no automatic integration runner. Use a new ScratchRoot for an independently fresh run, pass it consistently to the scripts, and do not reuse an old identical-version cache. Run fixture subsets sequentially; the final evidence index merges their individual records.

Baseline evaluated-properties, dependency-audit, database-image/cleanup and source-manifest snapshots were additional read-only commands described in the evidence README. Package and file SHA-256 values support identification; a later rerun can differ in timestamps, signatures, SDK-derived output, advisory feed, and measured timings. Review source/provenance and package payload rather than requiring identical zip hashes across tools.

### Primary-source applications

The references above were researched on the inspection date. They are linked next to the claims they support. Their repository-specific applications are:

| Primary source | Application here |
|---|---|
| NuGet PackageReference and source mapping | Public dependency flow; separate caches prevent same-version local/published contamination |
| Microsoft DI guidance | Scope ownership and disposal fix for the factory |
| Microsoft SqlClient pooling/retry/CommandTimeout | Preserve logical connection ownership; classify retries; timeout 0 is unlimited |
| Npgsql basic usage/performance | Data-source ownership and driver-aware measurement; no unsupported pool-performance claim |
| MySqlConnector best practices | Keep async I/O; do not invent a blanket synchronous accessor defect |
| Mongo client guide, version-matched compressor source, release notes | Client ownership, advisory reachability, tested upgrade requirement |
| Oracle [Size documentation](https://docs.oracle.com/en/database/oracle/oracle-database/26/odpnt/ParameterSize.html) | Native default/inference differs from neutral -1; output capacity and Oracle-specific semantics |
| Snowflake official connector | Native/platform/account requirements and explicit advanced capabilities |
| Microsoft breaking-change and package-validation guidance | Preserve released MySQL signatures and gate candidate API compatibility |

The SQL Server timeout statement is supported by [SqlCommand.CommandTimeout](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.sqlcommand.commandtimeout): zero is unlimited, whereas the driver's normal default is 30 seconds. The wrapper overwrites that default. This supports a new safer query default, not a silent patch change to every existing stored-procedure caller.

### Unresolved questions and conditional work

1. Which provider capabilities are formally supported beyond the current public NuGet packages? Oracle/Snowflake need an explicit release/support decision; this review does not authorize publication.
2. Which Snowflake account/authentication method and permissions can support reproducible tests? No credentials/account were available, so network/query/native-stage behavior remains blocked.
3. Which driver upgrade should replace MongoDB 3.5.2? Current releases/advisories were researched, but no upgrade candidate was executed.
4. Must legacy callers depend on mutable factory mappings, null/shape-varying results, old command defaults, or the removed MySQL parameter-name arguments? Preserve adapters and collect usage evidence before a major change.
5. Which caller-supplied connection/transaction, bulk, provider-specific type, and streaming features are actually needed? Add explicit capabilities with tests instead of guessing a universal abstraction.
6. What production payloads/concurrency/latency targets should guide performance work? Local mapping measurements do not establish a production bottleneck.
7. Linux/macOS client execution, trim/NativeAOT compatibility, sustained cursor/client resource growth, and real fault-injection retry outcomes remain unverified. Assembly scanning and expression compilation make trimming/AOT a conditional investigation; no successful AOT compatibility claim is made.

Every provider has a documented consumption result. All requested quality areas were assessed, with confirmed defects, source-based risks, simulations, and unavailable integration evidence kept distinct. The implementation backlog is proposed work; production source remains unchanged.

<!-- Source references pinned to the reviewed commit. -->
[s-core-package]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/DataAccessProvider.Core.csproj#L26
[s-mongo-package]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MongoDB/DataAccessProvider.MongoDB.csproj#L30
[s-oracle-package]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Oracle/DataAccessProvider.Oracle.csproj#L4
[s-snowflake-package]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Snowflake/DataAccessProvider.Snowflake.csproj#L4
[s-test-mssql]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/Test/Test_MSSQL.cs#L55
[s-mongo-options]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MongoDB/MongoDBSource.cs#L148
[s-overload]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.cs#L337
[s-typed-return]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.cs#L128
[s-mongo-typed]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MongoDB/MongoDBSource.cs#L265
[s-mongo-conversion]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MongoDB/MongoDBSource.cs#L486
[s-empty-result]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.cs#L455
[s-sql-parameter]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MSSQL/MSSQLSource.cs#L28
[s-nonquery]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.cs#L177
[s-factory-scope]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/DataSource/DataSourceFactory.cs#L53
[s-policy-hidden]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.cs#L631
[s-retry]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Resilience/BasicResiliencePolicy.cs#L32
[s-oracle-size]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Oracle/OracleSource.cs#L34
[s-neutral-parameter]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Types/DataAccessParameter.cs#L9
[s-source-interface]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Interfaces/IDataSource.cs#L1
[s-command-defaults]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSourceParams.cs#L15
[s-factory-map]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/DataSource/DataSourceFactory.cs#L24
[s-factory-params]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/DataSource/DataSourceFactory.cs#L87
[s-mongo-registration]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MongoDB/ServiceExtensions.cs#L48
[s-mysql-registration]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MySql/ServiceExtensions.cs#L50
[s-mongo-client]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MongoDB/MongoDBSource.cs#L20
[s-mysql-json]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MySql/DbParameterExtensions.cs#L42
[s-transaction-command]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.Transactions.cs#L192
[s-dictionary-map]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Extensions/DictionaryExtensions.cs#L53
[s-mapping]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.cs#L480
[s-raw-map]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.cs#L264
[s-mapping-error]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/Abstractions/BaseDatabaseSource.cs#L416
[s-release]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/.github/workflows/publish-nuget.yml#L38
[s-ci]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/.github/workflows/ci.yml#L1
[s-sql-doc]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.MSSQL/README.md#L78
[s-pg-doc]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Postgres/README.md#L8
[s-core-doc]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/README.md#L144
[s-file-source]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/DataSource/Source/JsonFileSource.cs#L23
[s-static-source]: https://github.com/habbs19/DataAccessProvider/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/DataAccessProvider.Core/DataSource/Source/StaticCodeSource.cs#L1

<!-- Local evidence files. -->
[e-consumers]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/consumer-matrix.json
[e-sql]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/integration-MSSQL-results.json
[e-pg]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/integration-POSTGRES-results.json
[e-mysql]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/integration-MYSQL-results.json
[e-mongo]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/integration-MONGO-results.json
[e-oracle]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/integration-ORACLE-results.json
[e-repro]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/reproduction-results.json
[e-source]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/source-manifest.json
[e-baseline]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/evaluated-baseline.json
[e-build]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/build.txt
[e-tests]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/tests.txt
[e-inventory]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/package-inventory.json
[e-dependencies]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/dependency-map.json
[e-experiment]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/metadata-experiment.json
[e-comparison]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/package-comparison.json
[e-minimum]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/published-minimum-matrix.json
[e-api-diff]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/public-api-comparison.json
[e-historical-api]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/historical-MYSQL-api.json
[e-historical-source]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/historical-MYSQL-declared-commit-source.cs.txt
[e-measurement]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/mapping-measurement-baseline.json
[e-example-checks]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/example-checks.json
[e-mongo-registration]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/hosted-registration-MONGO-run.txt
[e-audit]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/dependency-audit.json
[e-public-api]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/public-api.json
[e-all-docs]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/all-documentation-checks.json
[e-driver-api]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/public-driver-types.json
[e-host-source]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/HostedRegistrationProbe.cs
[e-corrected]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/postgres-corrected-query-and-transaction.cs.txt
[e-verification]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/verification-matrix.json
[e-images]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/database-images.json
[e-fixtures]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/integration-summary.json
[e-cleanup]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/fixture-cleanup.json
[e-evidence-readme]: C:/Users/habibs/source/repos/DataAccessProvider/review/evidence/README.md
