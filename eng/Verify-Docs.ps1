param([Parameter(Mandatory)][string]$ScratchRoot)
$ErrorActionPreference='Stop';$ScratchRoot=(Resolve-Path $ScratchRoot).Path;$repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$files=@(@{Path='README.md';Symbol='MSSQL'},@{Path='docs/migration.md';Symbol='MSSQL'})
$providers=@{Core='CORE';MSSQL='MSSQL';MySql='MYSQL';Postgres='POSTGRES';MongoDB='MONGO';Oracle='ORACLE';Snowflake='SNOWFLAKE'}
foreach($provider in $providers.Keys){$files+=@{Path="DataAccessProvider.$provider/README.md";Symbol=$providers[$provider]}}
$records=@()
foreach($file in $files) {
    $markdown=Get-Content (Join-Path $repo $file.Path) -Raw
    $fences=[regex]::Matches($markdown,'(?ms)^(?:```|~~~)csharp\s*\r?\n(.*?)(?:^```|^~~~)')
    for($i=0;$i -lt $fences.Count;$i++) {
        $symbol=if($fences[$i].Groups[1].Value -match 'using DataAccessProvider.MongoDB;'){'MONGO'}else{$file.Symbol}
        $case=Join-Path $ScratchRoot "docs/$($file.Path.Replace('/','-'))-$i";New-Item -ItemType Directory -Force -Path $case|Out-Null
        foreach($name in @('Consumer.csproj','NuGet.Config')){Copy-Item (Join-Path $ScratchRoot "consumers/local-$symbol/$name") $case}
        Set-Content (Join-Path $case 'Program.cs') $fences[$i].Groups[1].Value -Encoding utf8
        & dotnet restore (Join-Path $case 'Consumer.csproj') --configfile (Join-Path $case 'NuGet.Config') --packages (Join-Path $ScratchRoot "cache-$symbol") 2>&1|Set-Content (Join-Path $case 'restore.txt')
        if($LASTEXITCODE){throw "Documentation restore failed: $($file.Path) fence $i"}
        & dotnet build (Join-Path $case 'Consumer.csproj') -c Release --no-restore 2>&1|Set-Content (Join-Path $case 'build.txt')
        $records+=@{file=$file.Path;fence=$i;status=if($LASTEXITCODE){'failed'}else{'passed'}}
    }
}
$records|ConvertTo-Json|Set-Content (Join-Path $ScratchRoot 'docs-results.json')
if($records|Where-Object {$_.status -ne 'passed'}){throw 'Documentation compilation failed'}
Write-Host "$($records.Count) complete C# documentation examples compiled unchanged"
