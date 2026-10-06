# Reproduction evidence

This directory supports [the review](C:/Users/habibs/source/repos/DataAccessProvider/review/DataAccessProvider-review.md) of commit 0fe75f74d5c2e2254f7b08ef70f44e623a926fd8 on October 5, 2026. All source edits made during the task are review artifacts. Library sources and release configuration are unchanged.

## Prerequisites and isolation

- .NET 10 SDK; inspected SDK 10.0.400/runtime 10.0.11, Windows x64.
- NuGet.org access to obtain dependencies and published comparison packages.
- Docker for database checks; tested Linux amd64 images and digests are in database-images.json.
- Run from the reviewed repository commit. Run-PackageVerification.ps1 now checks HEAD before packing. It does not check out/reset anything.
- Use a **new** temporary ScratchRoot for independent freshness. The original run used C:/Users/habibs/AppData/Local/Temp/DataAccessProvider-review-0fe75f7.

Scripts write logs/JSON here and generated projects/packages/caches under ScratchRoot. The solution build also generates ordinary ignored bin/obj outputs. A new scratch folder is preferable to deleting user caches. Supply the same ScratchRoot to every script that accepts it.

~~~powershell
$scratch = Join-Path $env:TEMP ("DataAccessProvider-review-" + [guid]::NewGuid().ToString("N"))
& ./review/evidence/Run-PackageVerification.ps1 -ScratchRoot $scratch
& ./review/evidence/Run-PublishedMinimumCheck.ps1 -ScratchRoot $scratch
& ./review/evidence/Inspect-ApiAndDocs.ps1 -ScratchRoot $scratch
& ./review/evidence/Run-Reproductions.ps1 -ScratchRoot $scratch
& ./review/evidence/Run-ExampleChecks.ps1 -ScratchRoot $scratch
& ./review/evidence/Compile-AllDocExamples.ps1 -ScratchRoot $scratch
& ./review/evidence/Run-HistoricalApiCheck.ps1 -ScratchRoot $scratch
& ./review/evidence/Run-MetadataExperiment.ps1 -ScratchRoot $scratch
& ./review/evidence/Run-Integration.ps1 -ScratchRoot $scratch
& ./review/evidence/Build-EvidenceIndex.ps1
& ./review/evidence/Validate-Review.ps1 -ScratchRoot $scratch
& ./review/evidence/Build-EvidenceIndex.ps1
~~~

Expect negative scenarios to reproduce errors. These scripts collect evidence rather than failing the whole run on the first known defect. Check compiler exit codes, publicApiError records, exception records, result values, and verification-matrix.json.

## Artifact and script guide

| Artifact / script | Purpose and interpretation |
|---|---|
| sdk.txt, restore.txt, build.txt, tests.txt | Exact solution commands/output. Restore used a separate scratch package cache; existing tests were inspected before execution. 31 Core and 11 MSTest passed. |
| evaluated-baseline.json | Property-only MSBuild evaluation and package/project references for seven libraries, OS, architecture, inspection time and commit. Pack-time repository commit is captured separately in nuspecs. |
| source-manifest.json | SHA-256/line counts of tracked C#, project, solution, Markdown, JSON, config and workflow/editor files. It identifies reviewed source rather than reproducing every source file. |
| configuration-audit.json | Redacted configuration characteristics; no connection-string values. NuGet template placeholders were inspected separately. Not a full Git secret-history scan. |
| Run-PackageVerification.ps1 | Restores/builds/tests solution, explicitly packs seven libraries, obtains public version indexes/binaries, extracts manifests and creates clean consumers. Driver dependencies are never supplied directly. |
| ConsumerProbe.cs | Database-free constructors, driver assembly identity, Core identity and public signatures. REGISTRATION builds invoke the existing provider registration contract. Oracle/Snowflake have no such extension. No environment-supplied connection string is read. |
| pack-*.txt; local-*.nuspec.xml; package-inventory.json | Fresh pack commands, manifests, payload file lists and hashes. Copied payload binaries are in the scratch feed/cache. |
| published-versions.json; published-*.nuspec.xml | Public version indexes and accessible downloaded packages. Source version and latest stable were the same for five published IDs. Oracle/Snowflake public indexes returned 404. |
| package-comparison.json | Local/published dependency groups, repository commits, DLL/readme hashes. Different hashes alone are not proof of a particular behavioral difference. |
| consumer-matrix.json; local/published-*-restore/build/registration/run.txt | Ten independent one-provider cases: six local and four published. No project/Core/driver references or shared-framework references. Exit 0 on basic runs does not mean caught public API failures passed. |
| Run-PublishedMinimumCheck.ps1; published-minimum-matrix.json; published-minimum-*-assets/restore/build/run/registration files | Two supplementary plain one-provider consumers select published Core 1.3.6, which is the natural minimum for published MySQL/MongoDB. Another isolated feed/cache offers both 1.3.6 and 1.3.7. Both basic paths pass and DI closure/registration fail. Original comparison with only Core 1.3.7 had NU1603 warnings and remains separately recorded. |
| published-DataAccessProvider.Core-1.3.6.nuspec.xml; published-minimum-Core-package.json | Historical Core binary minimum, targeting net10.0, declared source commit 3b052143feb7f8d3df82d87054c6b04e6a7bae24. Not the reviewed source baseline. |
| local/published-*-assets.json; dependency-map.json | Original and normalized full versioned dependency graphs, compile/runtime/RID assets and direct dependencies. Separate local/published caches prevent identical-version artifact substitution. |
| local/published-POSTGRES-registration-run.txt | Explicit clean registration runtime calls; added after the original compile-only registration pass. No shared framework or database call. |
| Inspect-ApiAndDocs.ps1; public-api.json; public-api-comparison.json | Public signature inspection with a separately supplied host framework to load otherwise missing DI signatures. These are API/host checks, not clean package proofs. |
| public-driver-types.json | Driver-native and infrastructure signatures derived from local API reflection. Relational declared public driver-native signatures: zero; MongoDB: twelve. |
| Run-HistoricalApiCheck.ps1; PublishedMySqlApiProbe.cs | Inspects actual published MySQL optional parameter metadata and DLL hash; obtains source file from the commit declared in its nuspec. |
| historical-MYSQL-api.json; historical-MYSQL-declared-commit-source.cs.txt | Published binary has two five-argument AddJSONParams signatures, including optional operationLabel/paramsLabel. Declared-commit source has three-argument methods. Cause of this provenance mismatch is unknown; stale/uncommitted build inputs are hypotheses. |
| Run-Reproductions.ps1; ReproductionProbe.cs; reproduction-results.json | Package-based hosted fake ADO.NET/DI and owned-file reproductions, plus mapping measurements. No live connection destination. Temporary file is generated with a new GUID, written locally and deleted by the probe. |
| mapping-measurement-baseline.json | Retained measured run used in the review table, before the additional file probe rerun. Subsequent reproduction-results timings vary. Five operations per row count, warmed BCL reader, no database/network. |
| Run-ExampleChecks.ps1; HostedRegistrationProbe.cs; example-checks.json | Focused exact/corrected example compilation and database-free host registration. Separate cases explicitly use Microsoft.AspNetCore.App; underlying clean failures remain visible. Mongo null projection adapter is isolated directly. |
| documentation-checks.json; docs-*-excerpt/build/restore.txt | Initial exact provider registration fences with host context. Context was supplied for compilation only. |
| Compile-AllDocExamples.ps1; all-documentation-checks.json; *-fence-*-excerpt/build.txt | All 41 C#-labelled fences: 26 compiled, 11 failed, 4 displayed-output blocks not applicable. Four provider packages and framework supply context for cross-provider docs. Domain/host scaffolding is defined in the script. Usings/record declaration text are moved to valid scope; statements are preserved. No database execution. |
| postgres-corrected-query-and-transaction.cs.txt and build log | Corrected neutral parameter list, POCO with public parameterless constructor, explicit Text/timeout and transaction call compiled separately. Database behavior is verified in integration. |
| Run-MetadataExperiment.ps1; experimental-Core.nuspec.xml; metadata-experiment.json | Copies fresh packages into another feed, modifies ONLY Core's copied manifest to add DI Abstractions 10.0.1, uses a separate cache. All five affected basic/public-signature probes pass; three existing registrations run. Assemblies and production projects remain unchanged. Same-version modified packages are experimental and must never be published. |
| IntegrationProbe.cs; Run-Integration.ps1 | Owned disposable DB fixtures only. Numeric ephemeral port argument, fixed loopback host/fresh fixture DB/dummy credentials. No inherited DAP_REVIEW_CONNECTION or configurable remote destination. |
| integration-*-restore/build/results.json or .txt | Per-provider compile/runtime observations. A completed result can expose a defect (stale rows, unchanged output, dropped filter); it is not automatically passed. |
| integration-*-fixture.json; integration-summary.json; database-images.json | Fixture image/digest/ID/port/container identity and merged summary. Run subsets sequentially; independent per-provider files are authoritative and Build-EvidenceIndex reconstructs the summary. |
| integration-ORACLE-initial-startup.txt | First listener/service readiness failure, not a package failure. The downloaded image eventually ran; subsequent readiness-gated tests connected. |
| integration-MSSQL-initial-cancellation-observation.json | Initial observation surfaced driver SqlException for cancellation. A refined probe records requested token, original message, 229 ms interruption and post-cancellation health=true. It does not require every driver to throw OperationCanceledException. |
| integration-ORACLE-default-size-first.json | Earlier default-size failure before adding explicit-size workaround cases. Current integration-ORACLE-results.json is authoritative. |
| integration-ORACLE-container.txt; oracle-image-manifest.json; oracle-pull-progress.json | Oracle startup log, official image manifest and historical pull progress. Pull progress does not describe final verification status. |
| integration-POSTGRES-initial-probe-build.txt | Harness initially called a nonexistent convenience AddParameter overload. Corrected to neutral list construction; later integration succeeded. Not a provider package build defect. |
| integration-first-pass-summary.json | Historical incomplete intermediate summary from concurrent subset execution; do not use as final matrix. Per-provider files/final summary supersede it. |
| dependency-audit.json | Read-only NuGet vulnerable/include-transitive JSON, inspected date. Two advisories in Mongo graph; reachability assessed using version-matched upstream compressor source. |
| fixture-cleanup.json | Read-only Docker ownership-label check; final result zero remaining review containers. Image caches remain. |
| verification-matrix.json | Normalized package/database results: passed, failed, blocked, not applicable. Report adds documentation/reproduction areas and limitations. |
| Validate-Review.ps1; review-validation.json | Checks the 100 reviewed source hashes, report reference targets and source line bounds, JSON and PowerShell syntax, all 12 isolated one-provider projects, and tracked production changes. Final result passed. Run the index builder again afterward to include this validation record in the hashes. |
| evidence-sha256.json | Hash/size index of evidence files at final collection. Re-run Build-EvidenceIndex after generating/updating evidence. The index excludes itself. |

## Additional baseline capture commands

The evaluated snapshot was produced with this command pattern for each library:

~~~powershell
dotnet msbuild <library.csproj> -getProperty:TargetFramework,PackageId,PackageVersion,Version,AssemblyVersion,IsPackable,PackageReadmeFile,PackageLicenseExpression,RepositoryUrl,RepositoryCommit,Nullable,TreatWarningsAsErrors,IncludeBuildOutput,SuppressDependenciesWhenPacking,GenerateAssemblyInfo -getItem:PackageReference,ProjectReference
git rev-parse HEAD
dotnet --info
dotnet list DataAccessProvider.sln package --vulnerable --include-transitive --format json --no-restore
~~~

The source manifest hashes files selected from git ls-files with extensions .cs, .csproj, .sln, .md, .json, .config, .yml and .yaml, plus .editorconfig. Instruction/build-policy inventory used rg --files and ancestor-path checks; repository instructions were absent.

Image capture used docker image inspect on the five fixed fixture references. Cleanup capture used docker ps -a with label dap.review.commit=0fe75f7. These commands are read-only.

## Database lifecycle

Run-Integration creates names dap-review-0fe75f7-{provider}. It refuses a pre-existing name, records the newly returned container ID, validates both name and ownership label, and removes only that owned ID with its anonymous volumes in finally. It binds ports on 127.0.0.1, uses fresh fixture data and dummy credentials, and runs the local packed provider package with no direct driver package.

Oracle waits for first-boot health/service readiness. Image pulls and startup can take time. Failure before readiness is recorded separately from command failures after successful connection. Snowflake requires a test account; none was available, and no account/network operations were attempted.

Tests intentionally create tables/procedures and write/delete fixture data. They never inspect/use existing database credentials. SQL Server Developer is used only for this disposable test fixture. Do not repoint this probe at an existing database.

## Interpretation limits

Published binary checks and fresh database checks are independent. Published behavior was not comprehensively integrated against every database. Source hashes/provenance do not prove bit-for-bit reproducibility, especially where the declared MySQL commit differs from the binary API. Constructor/signature checks do not prove every OS/native/authentication feature. Database unavailability is blocked evidence, not a failed package and not a successful integration.

Current negative scenarios should remain failures until corresponding production fixes are implemented in a separate task. Proposed APIs in the report are not executable/current-library examples.
