$ErrorActionPreference='Stop'
$graphs=foreach($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*-assets.json'){
    $assets=Get-Content -LiteralPath $file.FullName -Raw|ConvertFrom-Json
    $target=$assets.targets.PSObject.Properties|Select-Object -First 1
    $resolved=@{};foreach($entry in $target.Value.PSObject.Properties){$parts=$entry.Name -split '/';$resolved[$parts[0]]=$parts[1]}
    $libraries=foreach($entry in $target.Value.PSObject.Properties){
        $parts=$entry.Name -split '/'
        @{id=$parts[0];version=$parts[1];dependencies=@($entry.Value.dependencies.PSObject.Properties|Where-Object {$null -ne $_ -and $_.Name}|ForEach-Object {@{id=$_.Name;requested=$_.Value;resolved=$resolved[$_.Name]}});compile=@($entry.Value.compile.PSObject.Properties.Name);runtime=@($entry.Value.runtime.PSObject.Properties.Name);runtimeTargets=@($entry.Value.runtimeTargets.PSObject.Properties|Where-Object {$null -ne $_ -and $_.Name}|ForEach-Object {@{path=$_.Name;type=$_.Value.assetType;rid=$_.Value.rid}})}
    }
    @{consumer=$file.BaseName;framework=$target.Name;libraries=@($libraries)}
}
$graphs|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'dependency-map.json') -Encoding utf8
$fixtures=@(Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'integration-*-fixture.json'|ForEach-Object {Get-Content -LiteralPath $_.FullName -Raw|ConvertFrom-Json})
$fixtures|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'integration-summary.json') -Encoding utf8
$matrix=@()
foreach($c in Get-Content (Join-Path $PSScriptRoot 'consumer-matrix.json') -Raw|ConvertFrom-Json){
    foreach($check in @('restore','build','run','registration')){$code=$c.$check;$matrix+=@{provider=$c.id;origin=$c.origin;area='package';check=$check;status=if($code -eq -1){'not applicable'}elseif($code -eq 0){'passed'}else{'failed'};artifact="$($c.origin)-$($c.id.Split('.')[-1]) (see consumer-matrix.json)"}}
    $symbol=switch($c.id.Split('.')[-1]){'MySql'{'MYSQL'}'PostgreSql'{'POSTGRES'}'MongoDB'{'MONGO'}'Oracle'{'ORACLE'}'Snowflake'{'SNOWFLAKE'}default{'MSSQL'}}
    $runFile="$($c.origin)-$symbol-run.txt";$runText=Get-Content (Join-Path $PSScriptRoot $runFile) -Raw
    $data=$runText.Substring($runText.IndexOf("`n[")+1)|ConvertFrom-Json
    $matrix+=@{provider=$c.id;origin=$c.origin;area='package';check='public assembly signature dependency closure';status=if(@($data|Where-Object name -EQ publicApiError).Count){'failed'}else{'passed'};artifact=$runFile}
}
$defects=@('reusedTypedEmptyRead','explicitTypedOverload','typedSourceInterfaceOverload','storedProcedureOutput','documentedSelectDefault','duplicateColumns','mappingErrorDisclosure','typedReadFromUntypedParams','typedReadWithProjectionAndSort','typedReadFromTypedParams','typedFilterDropped','anonymousBlockOutput','anonymousBlockOutputWithExplicitSize')
foreach($f in $fixtures){
    $file="integration-$($f.symbol)-results.json"
    foreach($r in Get-Content (Join-Path $PSScriptRoot $file) -Raw|ConvertFrom-Json){
        $status=if($r.name -in $defects -or $r.status -eq 'exception'){'failed'}else{'passed'}
        $matrix+=@{provider=$f.symbol;origin='fresh local package';area='database';check=$r.name;status=$status;artifact=$file;observation=$r.detail;error=$r.error}
    }
}
$matrix+=@{provider='SNOWFLAKE';area='database';check='account-dependent integration';status='blocked';reason='No available test account or credentials'}
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'published-minimum-matrix.json')){
    foreach($c in Get-Content (Join-Path $PSScriptRoot 'published-minimum-matrix.json') -Raw|ConvertFrom-Json){
        foreach($check in @('restore','build','run','registration')){$matrix+=@{provider=$c.symbol;origin=$c.origin;area='package';check=$check;status=if($c.$check -eq 0){'passed'}else{'failed'};artifact="published-minimum-$($c.symbol)-$check.txt";resolvedCore=$c.resolvedCore}}
        $file="published-minimum-$($c.symbol)-run.txt";$data=Get-Content (Join-Path $PSScriptRoot $file) -Raw|ConvertFrom-Json
        $matrix+=@{provider=$c.symbol;origin=$c.origin;area='package';check='public assembly signature dependency closure';status=if(@($data|Where-Object name -EQ publicApiError).Count){'failed'}else{'passed'};artifact=$file;resolvedCore=$c.resolvedCore}
    }
}
foreach($c in Get-Content (Join-Path $PSScriptRoot 'all-documentation-checks.json') -Raw|ConvertFrom-Json){$matrix+=@{area='documentation';check=$c.name;status=$c.status;source=$c.source;line=$c.line;artifact='all-documentation-checks.json'}}
$matrix|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'verification-matrix.json') -Encoding utf8
$hashes=Get-ChildItem -LiteralPath $PSScriptRoot -File|Where-Object Name -NE 'evidence-sha256.json'|ForEach-Object {@{name=$_.Name;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}}
$hashes|ConvertTo-Json -Depth 3|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'evidence-sha256.json') -Encoding utf8
