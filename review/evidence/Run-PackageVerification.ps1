param([string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
      [string]$ScratchRoot = (Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'))
$ErrorActionPreference = 'Stop'
$reviewedCommit='0fe75f74d5c2e2254f7b08ef70f44e623a926fd8'
$actualCommit=& git -C $RepoRoot rev-parse HEAD
if($actualCommit -ne $reviewedCommit){throw "Expected reviewed commit $reviewedCommit; found $actualCommit. Use a separate checkout of the reviewed source."}
$evidence = $PSScriptRoot
$feed = Join-Path $ScratchRoot 'local-feed'
$publishedFeed = Join-Path $ScratchRoot 'published-feed'
New-Item -ItemType Directory -Force -Path $feed,$publishedFeed | Out-Null
$packages = @(
    @{ Id='DataAccessProvider.Core'; Project='DataAccessProvider.Core'; Version='1.3.7'; Symbol='CORE' },
    @{ Id='DataAccessProvider.MSSQL'; Project='DataAccessProvider.MSSQL'; Version='1.3.7'; Symbol='MSSQL' },
    @{ Id='DataAccessProvider.MySql'; Project='DataAccessProvider.MySql'; Version='1.3.4'; Symbol='MYSQL' },
    @{ Id='DataAccessProvider.PostgreSql'; Project='DataAccessProvider.Postgres'; Version='1.3.4'; Symbol='POSTGRES' },
    @{ Id='DataAccessProvider.MongoDB'; Project='DataAccessProvider.MongoDB'; Version='1.3.3'; Symbol='MONGO' },
    @{ Id='DataAccessProvider.Oracle'; Project='DataAccessProvider.Oracle'; Version='1.0.0'; Symbol='ORACLE' },
    @{ Id='DataAccessProvider.Snowflake'; Project='DataAccessProvider.Snowflake'; Version='1.0.0'; Symbol='SNOWFLAKE' }
)
function Invoke-Dotnet([string]$Name, [string[]]$Arguments) {
    $output = & dotnet @Arguments 2>&1 | Out-String
    $exitCode = $LASTEXITCODE
    "dotnet $($Arguments -join ' ')`nExitCode: $exitCode`n$output" | Set-Content -LiteralPath (Join-Path $evidence "$Name.txt") -Encoding utf8
    Write-Host "$Name exit=$exitCode"
    return $exitCode
}
$null = Invoke-Dotnet 'sdk' @('--info')
$null = Invoke-Dotnet 'restore' @('restore',(Join-Path $RepoRoot 'DataAccessProvider.sln'),'--packages',(Join-Path $ScratchRoot 'packages'),'--source','https://api.nuget.org/v3/index.json','--force','--verbosity','minimal')
$null = Invoke-Dotnet 'build' @('build',(Join-Path $RepoRoot 'DataAccessProvider.sln'),'-c','Release','--no-restore','--verbosity','minimal')
$null = Invoke-Dotnet 'tests' @('test',(Join-Path $RepoRoot 'DataAccessProvider.sln'),'-c','Release','--no-build','--no-restore','--verbosity','minimal')
Add-Type -AssemblyName System.IO.Compression.FileSystem
function Describe-Package([string]$Path, [string]$Origin) {
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.Entries | Where-Object FullName -Like '*.nuspec' | Select-Object -First 1
        $reader = [IO.StreamReader]::new($entry.Open()); try { $xmlText=$reader.ReadToEnd() } finally { $reader.Dispose() }
        [xml]$spec=$xmlText
        $id=$spec.package.metadata.id; $version=$spec.package.metadata.version
        $xmlText | Set-Content -LiteralPath (Join-Path $evidence "$Origin-$id-$version.nuspec.xml") -Encoding utf8
        $entryHashes = @{}
        foreach($item in $archive.Entries | Where-Object { $_.FullName -like '*.dll' -or $_.FullName -eq 'README.md' }) {
            $stream=$item.Open(); $sha=[Security.Cryptography.SHA256]::Create()
            try { $entryHashes[$item.FullName]=([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant() } finally { $stream.Dispose();$sha.Dispose() }
        }
        return @{ origin=$Origin; id=$id; version=$version; packageSha256=(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash; files=@($archive.Entries.FullName); entryHashes=$entryHashes; dependencyGroups=@($spec.package.metadata.dependencies.group | ForEach-Object { @{ framework=$_.targetFramework; dependencies=@($_.dependency | ForEach-Object { @{id=$_.id;version=$_.version;exclude=$_.exclude} }) } }); repositoryCommit=$spec.package.metadata.repository.commit }
    } finally { $archive.Dispose() }
}
$inventory=@(); $publishedVersions=@()
foreach($package in $packages) {
    $project=Join-Path $RepoRoot "$($package.Project)\$($package.Project).csproj"
    $null=Invoke-Dotnet "pack-$($package.Symbol)" @('pack',$project,'-c','Release','--no-build','--no-restore','--output',$feed)
    $nupkg=Join-Path $feed "$($package.Id).$($package.Version).nupkg"
    if(Test-Path -LiteralPath $nupkg){ $inventory += Describe-Package $nupkg 'local' }
    $lowerId=$package.Id.ToLowerInvariant()
    try {
        $index=Invoke-RestMethod -Uri "https://api.nuget.org/v3-flatcontainer/$lowerId/index.json"
        $latest=@($index.versions | Where-Object { $_ -notlike '*-*' })[-1]
        $publishedVersions+=@{id=$package.Id;sourceVersion=$package.Version;latestStable=$latest;versions=@($index.versions);status='available'}
        foreach($version in @($package.Version,$latest) | Select-Object -Unique) {
            if($index.versions -contains $version) {
                $path=Join-Path $publishedFeed "$($package.Id).$version.nupkg"
                Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/$lowerId/$version/$lowerId.$version.nupkg" -OutFile $path -UseBasicParsing
                $inventory+=Describe-Package $path 'published'
            }
        }
    } catch { $publishedVersions+=@{id=$package.Id;sourceVersion=$package.Version;status='unavailable';error=$_.Exception.Message} }
}
$inventory | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $evidence 'package-inventory.json') -Encoding utf8
$publishedVersions | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidence 'published-versions.json') -Encoding utf8
$matrix=@()
foreach($origin in @('local','published')) {
    $originFeed=if($origin -eq 'local'){$feed}else{$publishedFeed}
    foreach($package in $packages | Where-Object Symbol -NE 'CORE') {
        if(!(Test-Path -LiteralPath (Join-Path $originFeed "$($package.Id).$($package.Version).nupkg"))){continue}
        $case=Join-Path $ScratchRoot "consumers\$origin-$($package.Symbol)"
        New-Item -ItemType Directory -Force -Path $case | Out-Null
        $cache=Join-Path $ScratchRoot "$origin-consumer-cache"
        @"
<configuration><packageSources><clear/><add key="review" value="$originFeed"/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="review"><package pattern="DataAccessProvider.*"/></packageSource><packageSource key="nuget"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
"@ | Set-Content -LiteralPath (Join-Path $case 'NuGet.Config') -Encoding utf8
        @"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><DefineConstants>$($package.Symbol);`$(ExtraProbeConstants)</DefineConstants></PropertyGroup><ItemGroup><PackageReference Include="$($package.Id)" Version="$($package.Version)" /></ItemGroup></Project>
"@ | Set-Content -LiteralPath (Join-Path $case 'Consumer.csproj') -Encoding utf8
        Copy-Item -LiteralPath (Join-Path $evidence 'ConsumerProbe.cs') -Destination (Join-Path $case 'Program.cs') -Force
        $project=Join-Path $case 'Consumer.csproj'
        $prefix="$origin-$($package.Symbol)"
        $restore=Invoke-Dotnet "$prefix-restore" @('restore',$project,'--configfile',(Join-Path $case 'NuGet.Config'),'--packages',$cache,'--force','--verbosity','minimal')
        $build=Invoke-Dotnet "$prefix-build" @('build',$project,'--no-restore','--verbosity','minimal')
        $run=if($build -eq 0){Invoke-Dotnet "$prefix-run" @('run','--project',$project,'--no-build','--no-restore')}else{-1}
        $registration=if($package.Symbol -in @('MSSQL','POSTGRES','MYSQL','MONGO')){Invoke-Dotnet "$prefix-registration" @('build',$project,'--no-restore','-p:ExtraProbeConstants=REGISTRATION','--verbosity','minimal')}else{-1}
        $registrationRun=if($registration -eq 0){Invoke-Dotnet "$prefix-registration-run" @('run','--project',$project,'--no-build','--no-restore')}else{-1}
        if(Test-Path -LiteralPath (Join-Path $case 'obj\project.assets.json')) {
            $assets=Get-Content -LiteralPath (Join-Path $case 'obj\project.assets.json') -Raw | ConvertFrom-Json
            @{libraries=$assets.libraries;targets=$assets.targets;directDependencies=$assets.project.frameworks} | ConvertTo-Json -Depth 40 | Set-Content -LiteralPath (Join-Path $evidence "$prefix-assets.json") -Encoding utf8
        }
        $matrix+=@{origin=$origin;id=$package.Id;version=$package.Version;restore=$restore;build=$build;run=$run;registration=$registration;registrationRun=$registrationRun;case=$case}
    }
}
$matrix | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidence 'consumer-matrix.json') -Encoding utf8
Write-Output ($matrix | ConvertTo-Json -Depth 5)
