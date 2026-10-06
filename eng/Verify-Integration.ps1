param([Parameter(Mandatory)][string]$ScratchRoot, [string[]]$Providers=@('MSSQL','POSTGRES','MYSQL','MONGO','ORACLE'), [switch]$MeasureStreamingMemory)
$ErrorActionPreference='Stop'
$ScratchRoot=(Resolve-Path $ScratchRoot).Path
$fixtures=@(
    @{Symbol='MSSQL';Image='mcr.microsoft.com/mssql/server@sha256:4402d880dd4c34bfa7d8705e56a86cd6c88da80a1f6bbbe741f999e76264a090';Port=1433;Env=@('ACCEPT_EULA=Y','MSSQL_PID=Developer','MSSQL_SA_PASSWORD=DapFixture_14!Database')},
    @{Symbol='POSTGRES';Image='postgres@sha256:721873c34ceb9f8d8fc265984940dc982404c105f19ad51be9fdc5970a6080ea';Port=5432;Env=@('POSTGRES_DB=dap_fixture','POSTGRES_USER=fixture','POSTGRES_PASSWORD=DapFixture_14!Database')},
    @{Symbol='MYSQL';Image='mysql@sha256:66aec17cd21a956029b83f083b813073859e8355dc1a00e55df6ba02f0e32345';Port=3306;Env=@('MYSQL_DATABASE=dap_fixture','MYSQL_USER=fixture','MYSQL_PASSWORD=DapFixture_14!Database','MYSQL_ROOT_PASSWORD=DapFixture_14!Root')},
    @{Symbol='MONGO';Image='mongo@sha256:d0d926f94df099bff534b7ee5b5986458131a22489dfff8664509af0c1e2ca9c';Port=27017;Env=@()},
    @{Symbol='ORACLE';Image='container-registry.oracle.com/database/free@sha256:cf540c3fa190d7cffad08c491652ac07fc70314e510f6b87890449517d565e94';Port=1521;Env=@('ORACLE_PWD=DapFixture_14!Database')}
)
$summary=@(); $owner=[guid]::NewGuid().ToString('N')
$previous=Join-Path $ScratchRoot 'integration-results.json'
if(Test-Path -LiteralPath $previous){$summary=@(Get-Content -LiteralPath $previous -Raw|ConvertFrom-Json|Where-Object {$_.provider -notin $Providers -and $_.provider -ne 'SNOWFLAKE'})}
foreach($fixture in $fixtures | Where-Object {$_.Symbol -in $Providers}) {
    $symbol=$fixture.Symbol; $name="dap-fixture-$owner-$($symbol.ToLowerInvariant())"; $container=$null
    $case=Join-Path $ScratchRoot "integration/$symbol";New-Item -ItemType Directory -Force -Path $case | Out-Null
    foreach($file in @('Consumer.csproj','NuGet.Config')) {Copy-Item -LiteralPath (Join-Path $ScratchRoot "consumers/local-$symbol/$file") -Destination $case}
    Set-Content (Join-Path $case 'Program.cs') (Get-Content (Join-Path $PSScriptRoot 'IntegrationProbe.cs') -Raw) -Encoding utf8
    try {
        & dotnet restore (Join-Path $case 'Consumer.csproj') --configfile (Join-Path $case 'NuGet.Config') --packages (Join-Path $ScratchRoot "cache-$symbol") 2>&1 | Set-Content (Join-Path $case 'restore.txt')
        if($LASTEXITCODE){throw 'Restore failed'}
        & dotnet build (Join-Path $case 'Consumer.csproj') -c Release --no-restore 2>&1 | Set-Content (Join-Path $case 'build.txt')
        if($LASTEXITCODE){throw 'Build failed'}
        $existing=& docker ps -a --filter "name=^/$name`$" --format '{{.ID}}';if($existing){throw 'Fixture identity already exists'}
        $dockerArgs=@('run','-d','--label',"dap.fixture.owner=$owner",'--name',$name,'-p',"127.0.0.1::$($fixture.Port)")
        foreach($setting in $fixture.Env){$dockerArgs+=@('-e',$setting)};$dockerArgs+=$fixture.Image
        if($symbol -eq 'MONGO'){$dockerArgs+=@('--replSet','dapFixture','--bind_ip_all')}
        $created=& docker @dockerArgs 2>&1 | Out-String;if($LASTEXITCODE){throw 'Container startup failed'}
        $container=$created.Trim();if($container -notmatch '^[0-9a-f]{64}$'){throw 'Invalid container identity'}
        $inspect=(& docker inspect $container | ConvertFrom-Json)[0]
        if($inspect.Config.Labels.'dap.fixture.owner' -ne $owner -or $inspect.Name -ne "/$name"){throw 'Fixture ownership mismatch'}
        $port=$inspect.NetworkSettings.Ports."$($fixture.Port)/tcp"[0].HostPort
        if($symbol -eq 'MONGO') {
            for($attempt=0;$attempt -lt 30;$attempt++) {
                & docker exec $container mongosh --quiet --eval 'rs.initiate({_id:"dapFixture",members:[{_id:0,host:"127.0.0.1:27017"}]})' *> $null
                if(!$LASTEXITCODE){break};Start-Sleep -Seconds 2
            }
            if($LASTEXITCODE){throw 'Replica-set initialization failed'}
        }
        if($symbol -eq 'ORACLE') {
            $deadline=(Get-Date).AddMinutes(6)
            do {$state=(& docker inspect $container | ConvertFrom-Json)[0];if($state.State.Health.Status -eq 'healthy' -or !$state.State.Running){break};Start-Sleep -Seconds 5} while((Get-Date) -lt $deadline)
        }
        & dotnet run --project (Join-Path $case 'Consumer.csproj') -c Release --no-build --no-restore -- $port 2>&1 | Set-Content (Join-Path $case 'results.json')
        $summary+=@{provider=$symbol;status=if($LASTEXITCODE){'failed'}else{'passed'};image=$fixture.Image}
        if($MeasureStreamingMemory -and $symbol -eq 'POSTGRES') {
            Set-Content (Join-Path $case 'Program.cs') (Get-Content (Join-Path $PSScriptRoot 'PeakMemoryProbe.cs') -Raw) -Encoding utf8
            & dotnet run --project (Join-Path $case 'Consumer.csproj') -c Release --no-restore -- $port 2>&1 | Set-Content (Join-Path $ScratchRoot 'streaming-memory.json')
            if($LASTEXITCODE){throw 'Streaming peak-memory gate failed'}
        }
        Write-Host "$symbol integration: $($summary[-1].status)"
    } catch {$summary+=@{provider=$symbol;status='blocked';reason=$_.Exception.Message};Write-Host "$symbol blocked: $($_.Exception.Message)"}
    finally {
        if($container -match '^[0-9a-f]{64}$') {
            $owned=(& docker inspect $container | ConvertFrom-Json)[0]
            if($owned.Config.Labels.'dap.fixture.owner' -eq $owner -and $owned.Name -eq "/$name") { & docker rm -f -v $container | Out-Null }
        }
    }
}
$summary+=@{provider='SNOWFLAKE';status='blocked';reason='Account-backed integration requires a disposable test account; use eng/SnowflakeProbe.cs.'}
$summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $ScratchRoot 'integration-results.json')
if($summary | Where-Object {$_.provider -ne 'SNOWFLAKE' -and $_.status -ne 'passed'}) {throw 'One or more disposable integration checks failed or were blocked'}
