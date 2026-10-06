param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'),[string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path)
$ErrorActionPreference='Stop'
$issues=@()
$report=Get-Content -LiteralPath (Join-Path $RepoRoot 'review/DataAccessProvider-review.md') -Raw
$definitions=[regex]::Matches($report,'(?m)^\[([^\]]+)\]:\s+(\S+)')
$labels=@($definitions|ForEach-Object {$_.Groups[1].Value})
foreach($reference in [regex]::Matches($report,'\[[^\]\r\n]+\]\[([^\]\r\n]+)\]')){if($reference.Groups[1].Value -notin $labels){$issues+="Undefined report reference: $($reference.Groups[1].Value)"}}
foreach($definition in $definitions){
    $target=$definition.Groups[2].Value
    if($target -like 'C:/*' -and !(Test-Path -LiteralPath $target)){$issues+="Missing target: $target"}
    if($target -match '/blob/0fe75f74d5c2e2254f7b08ef70f44e623a926fd8/(.*?)#L(\d+)$'){
        $source=Join-Path $RepoRoot $Matches[1];$line=[int]$Matches[2]
        if(!(Test-Path -LiteralPath $source)){$issues+="Missing source: $source"}
        elseif($line -gt @(Get-Content -LiteralPath $source).Count){$issues+="Source line out of bounds: $target"}
    }
}
$sourceRows=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'source-manifest.json') -Raw|ConvertFrom-Json
foreach($row in $sourceRows){if((Get-FileHash -LiteralPath (Join-Path $RepoRoot $row.path) -Algorithm SHA256).Hash -ne $row.sha256){$issues+="Reviewed source changed: $($row.path)"}}
foreach($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.json'){try{Get-Content -LiteralPath $file.FullName -Raw|ConvertFrom-Json|Out-Null}catch{$issues+="Invalid JSON: $($file.Name)"}}
foreach($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1'){
    $tokens=$null;$parseErrors=$null;[void][System.Management.Automation.Language.Parser]::ParseFile($file.FullName,[ref]$tokens,[ref]$parseErrors)
    foreach($error in $parseErrors){$issues+="Invalid PowerShell: $($file.Name): $($error.Message)"}
}
$cases=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'consumer-matrix.json') -Raw|ConvertFrom-Json|ForEach-Object {$_.case})
$cases+=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'published-minimum-matrix.json') -Raw|ConvertFrom-Json|ForEach-Object {Join-Path $ScratchRoot "consumers/published-minimum-$($_.symbol)"})
foreach($case in $cases){
    [xml]$project=Get-Content -LiteralPath (Join-Path $case 'Consumer.csproj') -Raw
    $references=@($project.Project.ItemGroup.PackageReference)
    if($references.Count -ne 1 -or $references[0].Include -notlike 'DataAccessProvider.*'){$issues+="Wrong direct packages: $case"}
    if($project.Project.ItemGroup.ProjectReference -or $project.Project.ItemGroup.FrameworkReference){$issues+="Extra project/shared framework reference: $case"}
    if($project.Project.PropertyGroup.TargetFramework -ne 'net10.0'){$issues+="Wrong framework: $case"}
}
$trackedChanges=@(& git -C $RepoRoot diff --name-only HEAD)
if($trackedChanges.Count){$issues+="Tracked changes: $($trackedChanges -join ', ')"}
$result=@{inspectionTime=(Get-Date).ToString('o');commit=(& git -C $RepoRoot rev-parse HEAD);sourceFilesChecked=$sourceRows.Count;singleProviderProjectsChecked=$cases.Count;referenceDefinitionsChecked=$definitions.Count;trackedProductionChanges=$trackedChanges;issues=$issues;status=if($issues.Count){'failed'}else{'passed'}}
$result|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'review-validation.json') -Encoding utf8
Write-Output ($result|ConvertTo-Json -Depth 5)
if($issues.Count){throw 'Review artifact validation failed'}
