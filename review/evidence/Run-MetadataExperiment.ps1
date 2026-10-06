param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'))
$ErrorActionPreference='Stop'
$feed=Join-Path $ScratchRoot 'experimental-feed';New-Item -ItemType Directory -Force -Path $feed|Out-Null
Get-ChildItem -LiteralPath (Join-Path $ScratchRoot 'local-feed') -Filter '*.nupkg'|Copy-Item -Destination $feed -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::Open((Join-Path $feed 'DataAccessProvider.Core.1.3.7.nupkg'),[IO.Compression.ZipArchiveMode]::Update)
try{
    $entry=$archive.Entries|Where-Object FullName -Like '*.nuspec'|Select-Object -First 1
    $name=$entry.FullName;$reader=[IO.StreamReader]::new($entry.Open());try{[xml]$spec=$reader.ReadToEnd()}finally{$reader.Dispose()}
    $dependency=$spec.CreateElement('dependency',$spec.DocumentElement.NamespaceURI)
    $dependency.SetAttribute('id','Microsoft.Extensions.DependencyInjection.Abstractions');$dependency.SetAttribute('version','10.0.1');$dependency.SetAttribute('exclude','Build,Analyzers')
    $null=$spec.package.metadata.dependencies.group.AppendChild($dependency)
    $spec.OuterXml|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'experimental-Core.nuspec.xml') -Encoding utf8
    $entry.Delete();$newEntry=$archive.CreateEntry($name);$writer=[IO.StreamWriter]::new($newEntry.Open());try{$writer.Write($spec.OuterXml)}finally{$writer.Dispose()}
}finally{$archive.Dispose()}
$results=@()
foreach($symbol in @('MSSQL','MYSQL','MONGO','ORACLE','SNOWFLAKE')){
    $case=Join-Path $ScratchRoot "metadata-experiment\$symbol";New-Item -ItemType Directory -Force -Path $case|Out-Null
    Copy-Item -LiteralPath (Join-Path $ScratchRoot "consumers\local-$symbol\Consumer.csproj") -Destination $case -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ConsumerProbe.cs') -Destination (Join-Path $case 'Program.cs') -Force
    $config=(Get-Content -LiteralPath (Join-Path $ScratchRoot "consumers\local-$symbol\NuGet.Config") -Raw).Replace((Join-Path $ScratchRoot 'local-feed'),$feed)
    $config|Set-Content -LiteralPath (Join-Path $case 'NuGet.Config') -Encoding utf8
    $project=Join-Path $case 'Consumer.csproj'
    & dotnet restore $project --configfile (Join-Path $case 'NuGet.Config') --packages (Join-Path $ScratchRoot 'experimental-cache') --verbosity minimal 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot "experiment-$symbol-restore.txt") -Encoding utf8
    $restore=$LASTEXITCODE
    & dotnet build $project --no-restore -p:ExtraProbeConstants=REGISTRATION --verbosity minimal 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot "experiment-$symbol-build.txt") -Encoding utf8
    $build=$LASTEXITCODE
    $run=-1
    if($build -eq 0){& dotnet run --project $project --no-build --no-restore 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot "experiment-$symbol-results.json") -Encoding utf8;$run=$LASTEXITCODE}
    $results+=@{symbol=$symbol;restore=$restore;build=$build;run=$run}
    Write-Host "metadata-experiment-$symbol build=$build run=$run"
}
$results|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'metadata-experiment.json') -Encoding utf8
