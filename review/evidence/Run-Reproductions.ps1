param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'))
$ErrorActionPreference='Stop'
$case=Join-Path $ScratchRoot 'reproductions'
New-Item -ItemType Directory -Force -Path $case | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ReproductionProbe.cs') -Destination (Join-Path $case 'Program.cs') -Force
Copy-Item -LiteralPath (Join-Path $ScratchRoot 'consumers\local-MSSQL\NuGet.Config') -Destination $case -Force
'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><PackageReference Include="DataAccessProvider.MSSQL" Version="1.3.7"/></ItemGroup></Project>' | Set-Content -LiteralPath (Join-Path $case 'Reproduction.csproj') -Encoding utf8
$project=Join-Path $case 'Reproduction.csproj'
& dotnet restore $project --configfile (Join-Path $case 'NuGet.Config') --packages (Join-Path $ScratchRoot 'local-consumer-cache') --verbosity minimal 2>&1 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'reproduction-restore.txt') -Encoding utf8
& dotnet build $project --no-restore --verbosity minimal 2>&1 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'reproduction-build.txt') -Encoding utf8
if($LASTEXITCODE -ne 0){throw 'Reproduction build failed'}
& dotnet run --project $project --no-build --no-restore 2>&1 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'reproduction-results.json') -Encoding utf8
Write-Output "Reproduction exit=$LASTEXITCODE"
