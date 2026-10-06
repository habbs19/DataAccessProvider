param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'),[string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path)
$ErrorActionPreference='Stop'
$case=Join-Path $ScratchRoot 'historical-api';New-Item -ItemType Directory -Force -Path $case|Out-Null
Copy-Item -LiteralPath (Join-Path $ScratchRoot 'api/published-MYSQL/Consumer.csproj'),(Join-Path $ScratchRoot 'api/published-MYSQL/NuGet.Config') -Destination $case -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'PublishedMySqlApiProbe.cs') -Destination (Join-Path $case 'Program.cs') -Force
$project=Join-Path $case 'Consumer.csproj'
& dotnet restore $project --configfile (Join-Path $case 'NuGet.Config') --packages (Join-Path $ScratchRoot 'published-consumer-cache') --verbosity minimal 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'historical-MYSQL-restore.txt') -Encoding utf8
& dotnet build $project --no-restore --verbosity minimal 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'historical-MYSQL-build.txt') -Encoding utf8
if($LASTEXITCODE -ne 0){throw 'Historical API build failed'}
& dotnet run --project $project --no-build --no-restore 2>&1|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'historical-MYSQL-api.json') -Encoding utf8
& git -C $RepoRoot show '41273661c05a6a96be19e7cf8657254cfd6720cb:DataAccessProvider.MySql/DbParameterExtensions.cs' | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'historical-MYSQL-declared-commit-source.cs.txt') -Encoding utf8
Write-Output "Historical API exit=$LASTEXITCODE"
