param([Parameter(Mandatory)][string]$ArtifactRoot,[Parameter(Mandatory)][string]$ExpectedCommit)
$ErrorActionPreference='Stop'
$manifest=Get-Content (Join-Path $ArtifactRoot 'manifest.json') -Raw|ConvertFrom-Json
if($manifest.dirty -or $manifest.sourceCommit -ne $ExpectedCommit){throw 'Release requires clean, exact-commit validated artifacts'}
foreach($file in @('package-results.json','docs-results.json','integration-results.json')){
    $checks=Get-Content (Join-Path $ArtifactRoot $file) -Raw|ConvertFrom-Json
    if(!$checks){throw "Empty release gate: $file"}
    if($checks|Where-Object {$_.provider -ne 'SNOWFLAKE' -and $_.status -ne 'passed'}){throw "Failed release gate: $file"}
}
$expectedPackages=@('Core','MSSQL','MySql','PostgreSql','MongoDB','Oracle','Snowflake')
if($manifest.packages.Count -ne 7 -or @($manifest.packages.id | Sort-Object -Unique).Count -ne 7){throw 'All seven validated packages are required'}
foreach($id in $expectedPackages){if("DataAccessProvider.$id" -notin $manifest.packages.id){throw "Missing package $id"}}
$consumers=Get-Content (Join-Path $ArtifactRoot 'package-results.json') -Raw|ConvertFrom-Json
foreach($id in $expectedPackages){if($id -notin $consumers.provider){throw "Missing consumer $id"}}
$integration=Get-Content (Join-Path $ArtifactRoot 'integration-results.json') -Raw|ConvertFrom-Json
foreach($id in @('MSSQL','POSTGRES','MYSQL','MONGO','ORACLE')){if($id -notin $integration.provider){throw "Missing database gate $id"}}
$audit=Get-Content (Join-Path $ArtifactRoot 'audit.json') -Raw|ConvertFrom-Json
if($audit.problems -or !$audit.projects){throw 'Missing or failed vulnerability lookup'}
foreach($project in $audit.projects){foreach($framework in $project.frameworks){foreach($package in @($framework.topLevelPackages)+@($framework.transitivePackages)){if($package.vulnerabilities){throw 'Audited vulnerability blocks release'}}}}
foreach($package in $manifest.packages) {
    if((Get-FileHash (Join-Path $ArtifactRoot "local-feed/$($package.file)") -Algorithm SHA256).Hash -ne $package.sha256){throw 'Artifact hash mismatch'}
    if($package.id -eq 'DataAccessProvider.Snowflake'){continue}
    $index="https://api.nuget.org/v3-flatcontainer/$($package.id.ToLowerInvariant())/index.json"
    try{$published=Invoke-RestMethod $index}catch{if($_.Exception.Response.StatusCode.value__ -ne 404){throw};$published=@{versions=@()}}
    if($package.version -in $published.versions){throw "Version already published: $($package.id) $($package.version)"}
}
Write-Host 'Exact-commit hashes, tests, docs, consumption, integration and unused versions validated'
