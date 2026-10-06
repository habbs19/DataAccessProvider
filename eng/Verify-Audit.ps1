param([string]$OutputPath='./artifacts/audit.json')
$ErrorActionPreference='Stop';$repo=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$parent=Split-Path $OutputPath -Parent;if($parent){New-Item -ItemType Directory -Force $parent|Out-Null}
& dotnet list (Join-Path $repo 'DataAccessProvider.sln') package --vulnerable --include-transitive --format json 2>&1|Set-Content $OutputPath
if($LASTEXITCODE){throw 'Vulnerability lookup failed'}
$audit=Get-Content $OutputPath -Raw|ConvertFrom-Json
if($audit.problems){throw 'Audit returned lookup problems'}
foreach($project in $audit.projects) {foreach($framework in $project.frameworks){foreach($package in @($framework.topLevelPackages)+@($framework.transitivePackages)){if($package.vulnerabilities){throw "Unaccepted vulnerability in $($package.id)"}}}}
Write-Host 'No known advisory in the resolved graph at inspection time'
