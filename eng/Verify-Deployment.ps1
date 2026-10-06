param([Parameter(Mandatory)][string]$ScratchRoot, [string]$Runtime='win-x64', [switch]$NativeAot)
$ErrorActionPreference='Stop';$ScratchRoot=(Resolve-Path $ScratchRoot).Path
$records=@()
foreach($symbol in @('CORE','MSSQL','MYSQL','POSTGRES','MONGO','ORACLE','SNOWFLAKE')) {
    $case=Join-Path $ScratchRoot "deployment/$symbol";New-Item -ItemType Directory -Force $case|Out-Null
    foreach($file in @('Consumer.csproj','NuGet.Config','Program.cs')){Copy-Item (Join-Path $ScratchRoot "consumers/local-$symbol/$file") $case}
    # Deployment probes construct/register only; the separate API census deliberately uses unbounded reflection.
    $program=Get-Content (Join-Path $case 'Program.cs') -Raw
    $program=[regex]::Replace($program,'(?m)^foreach \(var assembly .*\r?\nforeach \(var type .*\r?\n','')
    Set-Content (Join-Path $case 'Program.cs') $program -Encoding utf8
    $project=Join-Path $case 'Consumer.csproj'
    $properties=@(if($NativeAot){'-p:PublishAot=true'}else{'-p:PublishTrimmed=true'})
    & dotnet publish $project -c Release -r $Runtime --self-contained true --configfile (Join-Path $case 'NuGet.Config') "-p:RestorePackagesPath=$(Join-Path $ScratchRoot "cache-$symbol")" @properties -o (Join-Path $case 'publish') 2>&1|Set-Content (Join-Path $case 'publish.txt')
    $exit=$LASTEXITCODE
    $warnings=@(Select-String -Path (Join-Path $case 'publish.txt') -Pattern 'warning (IL|AOT)\d+'|ForEach-Object {$_.Line}|Sort-Object -Unique)
    $run=$null
    if(!$exit){$executable=Join-Path $case "publish/Consumer$(if($Runtime -like 'win-*'){'.exe'})";& $executable 2>&1|Set-Content (Join-Path $case 'run.txt');$run=$LASTEXITCODE}
    $records+=@{provider=$symbol;publishExit=$exit;runExit=$run;warnings=$warnings;status=if($exit -or $run -or $warnings.Count){'not supported as a general contract'}else{'passed limited construction probe'}}
}
$records|ConvertTo-Json -Depth 5|Set-Content (Join-Path $ScratchRoot "deployment-$(if($NativeAot){'aot'}else{'trim'})-results.json")
