param([string]$ScratchRoot = (Join-Path $env:TEMP ('dap-implementation-' + [guid]::NewGuid().ToString('N'))), [switch]$SkipPack)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
New-Item -ItemType Directory -Force -Path $ScratchRoot | Out-Null; $ScratchRoot = (Resolve-Path $ScratchRoot).Path; $feed = Join-Path $ScratchRoot 'local-feed'
New-Item -ItemType Directory -Force -Path $feed | Out-Null
$specs = @(
    @{Id='Core';Dir='Core';Symbol='CORE'},
    @{Id='MSSQL';Dir='MSSQL';Symbol='MSSQL';Ctor='SqlServerClient';Method='MSSQL';Marker='SqlServer';Connection='Server=localhost;Database=fixture;Encrypt=false'},
    @{Id='MySql';Dir='MySql';Symbol='MYSQL';Ctor='MySqlClient';Method='MySql';Marker='MySql';Connection='Server=localhost;Database=fixture'},
    @{Id='PostgreSql';Dir='Postgres';Symbol='POSTGRES';Ctor='PostgreSqlClient';Method='Postgres';Marker='PostgreSql';Connection='Host=localhost;Database=fixture'},
    @{Id='MongoDB';Dir='MongoDB';Symbol='MONGO';Ctor='MongoDocumentClient';Method='MongoDB';Connection='mongodb://localhost/fixture'},
    @{Id='Oracle';Dir='Oracle';Symbol='ORACLE';Ctor='OracleClient';Method='Oracle';Marker='OracleDatabase';Connection='User Id=fixture;Password=dummy;Data Source=localhost/FREEPDB1'},
    @{Id='Snowflake';Dir='Snowflake';Symbol='SNOWFLAKE';Ctor='SnowflakeClient';Method='Snowflake';Marker='SnowflakeDatabase';Connection='account=fixture;user=fixture;password=dummy;db=fixture;warehouse=fixture'}
)
$records = @()
foreach($p in $specs) {
    $project = Join-Path $repo "DataAccessProvider.$($p.Dir)/DataAccessProvider.$($p.Dir).csproj"
    if (!$SkipPack) {
        & dotnet pack $project -c Release --no-build --no-restore -o $feed 2>&1 | Set-Content (Join-Path $ScratchRoot "pack-$($p.Symbol).txt")
        if ($LASTEXITCODE) { throw "Pack failed: $($p.Id)" }
    }
}
foreach($p in $specs) {
    $case = Join-Path $ScratchRoot "consumers/local-$($p.Symbol)"
    New-Item -ItemType Directory -Force -Path $case | Out-Null
    $project = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><DefineConstants>'+ $p.Symbol +'</DefineConstants></PropertyGroup><ItemGroup><PackageReference Include="DataAccessProvider.'+$p.Id+'" Version="1.4.0" /></ItemGroup></Project>'
    Set-Content (Join-Path $case 'Consumer.csproj') $project -Encoding utf8
    $escapedFeed = [Security.SecurityElement]::Escape($feed)
    $config = '<configuration><packageSources><clear/><add key="local" value="'+$escapedFeed+'"/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="local"><package pattern="DataAccessProvider.*"/></packageSource><packageSource key="nuget"><package pattern="*"/></packageSource></packageSourceMapping></configuration>'
    Set-Content (Join-Path $case 'NuGet.Config') $config -Encoding utf8
    $resolve = if ($p.Symbol -eq 'MONGO') {'IDocumentClient'} else {'IDatabaseClient<'+$p.Marker+'>'}
    $program = if ($p.Symbol -eq 'CORE') { @'
using DataAccessProvider.Core;
using DataAccessProvider.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;
await using var host = new ServiceCollection().AddDataAccessProviderCore().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
var value = await new StaticValueSource<int>(42).ReadAsync();
if (value != 42) throw new Exception("Static result mismatch");
Console.WriteLine("Core standalone DI passed");
'@ } else { @"
using DataAccessProvider.Core;
using DataAccessProvider.$($p.Dir);
using Microsoft.Extensions.DependencyInjection;
await using var direct = new $($p.Ctor)("$($p.Connection)");
var services = new ServiceCollection().AddDataAccessProvider$($p.Method)("$($p.Connection)");
await using var host = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
await using var scope = host.CreateAsyncScope();
var client = scope.ServiceProvider.GetRequiredService<$resolve>();
if (!ReferenceEquals(client, scope.ServiceProvider.GetRequiredService<$($p.Ctor)>())) throw new Exception("Alias mismatch");
foreach (var assembly in new[] { typeof(DatabaseCommand).Assembly, typeof($($p.Ctor)).Assembly })
foreach (var type in assembly.GetExportedTypes()) { _ = type.GetMethods(); _ = type.GetConstructors(); _ = type.GetProperties(); }
Console.WriteLine("$($p.Id) standalone construction, registration and public API loading passed");
"@ }
    Set-Content (Join-Path $case 'Program.cs') $program -Encoding utf8
    $cache = Join-Path $ScratchRoot "cache-$($p.Symbol)"
    & dotnet restore (Join-Path $case 'Consumer.csproj') --configfile (Join-Path $case 'NuGet.Config') --packages $cache 2>&1 | Set-Content (Join-Path $ScratchRoot "restore-$($p.Symbol).txt")
    if ($LASTEXITCODE) { throw "Consumer restore failed: $($p.Id)" }
    & dotnet run --project (Join-Path $case 'Consumer.csproj') -c Release --no-restore 2>&1 | Set-Content (Join-Path $ScratchRoot "run-$($p.Symbol).txt")
    if ($LASTEXITCODE) { throw "Consumer build/run failed: $($p.Id)" }
    Copy-Item (Join-Path $case 'obj/project.assets.json') (Join-Path $ScratchRoot "assets-$($p.Symbol).json")
    $records += @{provider=$p.Id;status='passed';project=(Join-Path $case 'Consumer.csproj');cache=$cache}
    Write-Host "$($p.Id): passed"
}
$records | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $ScratchRoot 'package-results.json') -Encoding utf8
$packages=@()
foreach($file in Get-ChildItem $feed -Filter '*.nupkg') {
    $zip=[IO.Compression.ZipFile]::OpenRead($file.FullName)
    try {
        $entry=$zip.Entries | Where-Object {$_.FullName -like '*.nuspec'} | Select-Object -First 1
        $reader=[IO.StreamReader]::new($entry.Open());try{$xml=[xml]$reader.ReadToEnd()}finally{$reader.Dispose()}
        $metadata=$xml.package.metadata
        if(!$metadata.license -or !$metadata.readme -or !$metadata.repository.url){throw "Missing package metadata: $($file.Name)"}
        $packages+=@{file=$file.Name;id=$metadata.id;version=$metadata.version;sha256=(Get-FileHash $file.FullName -Algorithm SHA256).Hash;dependencies=@($metadata.dependencies.group|ForEach-Object {@{framework=$_.targetFramework;packages=@($_.dependency|ForEach-Object {@{id=$_.id;version=$_.version;exclude=$_.exclude}})}});assets=@($zip.Entries.FullName);repository=$metadata.repository.url}
    }finally{$zip.Dispose()}
}
$commit=& git -C $repo rev-parse HEAD
$dirty=[bool](& git -C $repo status --porcelain)
$manifest=@{sourceCommit=$commit;dirty=$dirty;sdk=(& dotnet --version);utc=[DateTime]::UtcNow.ToString('O');packages=$packages;packageConsumers=$records; snowflakeIntegration='blocked: account required'}
$manifest|ConvertTo-Json -Depth 12|Set-Content (Join-Path $ScratchRoot 'manifest.json') -Encoding utf8
Write-Host "Evidence: $ScratchRoot"
