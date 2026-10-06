param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'))
$ErrorActionPreference='Stop'
$feed=Join-Path $ScratchRoot 'published-minimum-feed';New-Item -ItemType Directory -Force -Path $feed|Out-Null
Get-ChildItem -LiteralPath (Join-Path $ScratchRoot 'published-feed') -Filter '*.nupkg'|Copy-Item -Destination $feed -Force
$core=Join-Path $feed 'DataAccessProvider.Core.1.3.6.nupkg'
Invoke-WebRequest -Uri 'https://api.nuget.org/v3-flatcontainer/dataaccessprovider.core/1.3.6/dataaccessprovider.core.1.3.6.nupkg' -OutFile $core -UseBasicParsing
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::OpenRead($core)
try {
    $entry=$archive.Entries|Where-Object FullName -Like '*.nuspec'|Select-Object -First 1
    $reader=[IO.StreamReader]::new($entry.Open());try{$xml=$reader.ReadToEnd()}finally{$reader.Dispose()}
    $xml|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'published-DataAccessProvider.Core-1.3.6.nuspec.xml') -Encoding utf8
    @{origin='published historical minimum';version='1.3.6';sha256=(Get-FileHash -LiteralPath $core -Algorithm SHA256).Hash;files=@($archive.Entries.FullName)}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'published-minimum-Core-package.json') -Encoding utf8
}finally{$archive.Dispose()}
$results=@()
foreach($symbol in @('MYSQL','MONGO')){
    $case=Join-Path $ScratchRoot "consumers/published-minimum-$symbol";New-Item -ItemType Directory -Force -Path $case|Out-Null
    Copy-Item -LiteralPath (Join-Path $ScratchRoot "consumers/published-$symbol/Consumer.csproj") -Destination $case -Force
    $config=(Get-Content -LiteralPath (Join-Path $ScratchRoot "consumers/published-$symbol/NuGet.Config") -Raw).Replace((Join-Path $ScratchRoot 'published-feed'),$feed)
    $config|Set-Content -LiteralPath (Join-Path $case 'NuGet.Config') -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ConsumerProbe.cs') -Destination (Join-Path $case 'Program.cs') -Force
    $project=Join-Path $case 'Consumer.csproj';$prefix="published-minimum-$symbol"
    & dotnet restore $project --configfile (Join-Path $case 'NuGet.Config') --packages (Join-Path $ScratchRoot 'published-minimum-cache') --force --verbosity minimal 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$prefix-restore.txt") -Encoding utf8
    $restore=$LASTEXITCODE
    & dotnet build $project --no-restore --verbosity minimal 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$prefix-build.txt") -Encoding utf8
    $build=$LASTEXITCODE;$run=-1
    if($build -eq 0){& dotnet run --project $project --no-build --no-restore 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$prefix-run.txt") -Encoding utf8;$run=$LASTEXITCODE}
    & dotnet build $project --no-restore -p:ExtraProbeConstants=REGISTRATION --verbosity minimal 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$prefix-registration.txt") -Encoding utf8
    $registration=$LASTEXITCODE
    $assets=Get-Content -LiteralPath (Join-Path $case 'obj/project.assets.json') -Raw|ConvertFrom-Json
    @{libraries=$assets.libraries;targets=$assets.targets;directDependencies=$assets.project.frameworks}|ConvertTo-Json -Depth 40|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$prefix-assets.json") -Encoding utf8
    $results+=@{origin='published-minimum';symbol=$symbol;restore=$restore;build=$build;run=$run;registration=$registration;resolvedCore=@($assets.libraries.PSObject.Properties.Name|Where-Object {$_ -like 'DataAccessProvider.Core/*'})}
    Write-Host "$prefix build=$build run=$run registration=$registration"
}
$results|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'published-minimum-matrix.json') -Encoding utf8
