param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'),[string[]]$Providers=@('MSSQL','POSTGRES','MYSQL','MONGO','ORACLE'))
$ErrorActionPreference='Stop'
$fixtures=@(
    @{Symbol='MSSQL';Image='mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04';Port=1433;Env=@('ACCEPT_EULA=Y','MSSQL_PID=Developer','MSSQL_SA_PASSWORD=DapReview_0fe75f7!Database')},
    @{Symbol='POSTGRES';Image='postgres:16-alpine';Port=5432;Env=@('POSTGRES_DB=dap_review','POSTGRES_USER=review','POSTGRES_PASSWORD=DapReview_0fe75f7!Database')},
    @{Symbol='MYSQL';Image='mysql:latest';Port=3306;Env=@('MYSQL_DATABASE=dap_review','MYSQL_USER=review','MYSQL_PASSWORD=DapReview_0fe75f7!Database','MYSQL_ROOT_PASSWORD=DapReview_0fe75f7!Root')},
    @{Symbol='MONGO';Image='mongo:8.0';Port=27017;Env=@()},
    @{Symbol='ORACLE';Image='container-registry.oracle.com/database/free:latest-lite';Port=1521;Env=@('ORACLE_PWD=DapReview_0fe75f7!Database')}
)
$summary=@()
if(Test-Path -LiteralPath (Join-Path $PSScriptRoot 'integration-summary.json')){$summary=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'integration-summary.json') -Raw | ConvertFrom-Json | Where-Object {$_.symbol -notin $Providers})}
foreach($fixture in $fixtures | Where-Object {$_.Symbol -in $Providers}) {
    $symbol=$fixture.Symbol
    $name="dap-review-0fe75f7-$($symbol.ToLowerInvariant())"
    $existing=& docker ps -a --filter "name=^/$name`$" --format '{{.Names}}'
    if($existing){throw "Fixture container name already exists: $name"}
    $case=Join-Path $ScratchRoot "integration\$symbol"
    New-Item -ItemType Directory -Force -Path $case | Out-Null
    Copy-Item -LiteralPath (Join-Path $ScratchRoot "consumers\local-$symbol\NuGet.Config") -Destination $case -Force
    Copy-Item -LiteralPath (Join-Path $ScratchRoot "consumers\local-$symbol\Consumer.csproj") -Destination $case -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'IntegrationProbe.cs') -Destination (Join-Path $case 'Program.cs') -Force
    $project=Join-Path $case 'Consumer.csproj'
    & dotnet restore $project --configfile (Join-Path $case 'NuGet.Config') --packages (Join-Path $ScratchRoot 'local-consumer-cache') --verbosity minimal 2>&1 | Set-Content -LiteralPath (Join-Path $PSScriptRoot "integration-$symbol-restore.txt") -Encoding utf8
    & dotnet build $project --no-restore --verbosity minimal 2>&1 | Set-Content -LiteralPath (Join-Path $PSScriptRoot "integration-$symbol-build.txt") -Encoding utf8
    if($LASTEXITCODE -ne 0){$summary+=@{symbol=$symbol;status='build failed'};continue}
    $dockerArgs=@('run','-d','--label','dap.review.commit=0fe75f7','--name',$name,'-p',"127.0.0.1::$($fixture.Port)")
    foreach($setting in $fixture.Env){$dockerArgs+=@('-e',$setting)}
    $dockerArgs+=$fixture.Image
    $container=$null
    try {
        $container=& docker @dockerArgs 2>&1 | Out-String
        if($LASTEXITCODE -ne 0){throw $container}
        $container=($container.Trim() -split "`n")[-1].Trim()
        if($container -notmatch '^[0-9a-f]{64}$'){throw 'Unexpected container identity'}
        $inspect=(& docker inspect $container | ConvertFrom-Json)[0]
        if($inspect.Config.Labels.'dap.review.commit' -ne '0fe75f7' -or $inspect.Name -ne "/$name"){throw 'Fixture ownership validation failed'}
        $port=$inspect.NetworkSettings.Ports."$($fixture.Port)/tcp"[0].HostPort
        if($symbol -eq 'ORACLE') {
            # The listener starts before FREEPDB1 is ready on a first boot.
            $deadline=(Get-Date).AddMinutes(6)
            do {
                $state=(& docker inspect $container | ConvertFrom-Json)[0]
                if($state.State.Health.Status -eq 'healthy'){break}
                if(!$state.State.Running){break}
                Start-Sleep -Seconds 5
            } while((Get-Date) -lt $deadline)
            & docker logs $container 2>&1 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'integration-ORACLE-container.txt') -Encoding utf8
        }
        & dotnet run --project $project --no-build --no-restore -- $port 2>&1 | Set-Content -LiteralPath (Join-Path $PSScriptRoot "integration-$symbol-results.json") -Encoding utf8
        $exitCode=$LASTEXITCODE
        $record=@{symbol=$symbol;status=if($exitCode -eq 0){'ran'}else{'runtime failed'};exitCode=$exitCode;image=$fixture.Image;imageId=$inspect.Image;port=$port;container=$container}
        $summary+=$record
        $record|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $PSScriptRoot "integration-$symbol-fixture.json") -Encoding utf8
        Write-Host "integration-$symbol exit=$exitCode"
    } catch {
        $summary+=@{symbol=$symbol;status='blocked';error=$_.Exception.Message}
        Write-Host "integration-$symbol blocked: $($_.Exception.Message)"
    } finally {
        if($container -match '^[0-9a-f]{64}$'){
            $owned=(& docker inspect $container | ConvertFrom-Json)[0]
            if($owned.Config.Labels.'dap.review.commit' -eq '0fe75f7' -and $owned.Name -eq "/$name") {
                & docker rm -f -v $container | Out-Null
            }
        }
    }
}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'integration-summary.json') -Encoding utf8
