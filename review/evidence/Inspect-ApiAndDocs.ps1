param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'),[string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,[switch]$DocsOnly)
$ErrorActionPreference='Stop'
$apiResults=@()
function Run-Captured([string]$Name,[string[]]$Arguments){
    $out=& dotnet @Arguments 2>&1 | Out-String; $code=$LASTEXITCODE
    "dotnet $($Arguments -join ' ')`nExitCode: $code`n$out" | Set-Content -LiteralPath (Join-Path $PSScriptRoot "$Name.txt") -Encoding utf8
    return @{code=$code;text=$out}
}
if(!$DocsOnly){ foreach($origin in @('local','published')){
    foreach($symbol in @('MSSQL','MYSQL','POSTGRES','MONGO','ORACLE','SNOWFLAKE')){
        $inputCase=Join-Path $ScratchRoot "consumers\$origin-$symbol"
        if(!(Test-Path -LiteralPath $inputCase)){continue}
        $case=Join-Path $ScratchRoot "api\$origin-$symbol"; New-Item -ItemType Directory -Force -Path $case | Out-Null
        Copy-Item -LiteralPath (Join-Path $inputCase 'NuGet.Config') -Destination $case -Force
        $projectText=Get-Content -LiteralPath (Join-Path $inputCase 'Consumer.csproj') -Raw
        $projectText.Replace('<ItemGroup>','<ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/>') | Set-Content -LiteralPath (Join-Path $case 'Consumer.csproj') -Encoding utf8
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ConsumerProbe.cs') -Destination (Join-Path $case 'Program.cs') -Force
        $project=Join-Path $case 'Consumer.csproj'; $prefix="api-$origin-$symbol"
        $null=Run-Captured "$prefix-restore" @('restore',$project,'--configfile',(Join-Path $case 'NuGet.Config'),'--packages',(Join-Path $ScratchRoot "$origin-consumer-cache"),'--verbosity','minimal')
        $build=Run-Captured "$prefix-build" @('build',$project,'--no-restore','--verbosity','minimal')
        if($build.code -eq 0){$run=Run-Captured "$prefix-run" @('run','--project',$project,'--no-build','--no-restore');if($run.code -eq 0){$parsed=$run.text.Trim()|ConvertFrom-Json;$apiResults+=@{origin=$origin;symbol=$symbol;records=$parsed}}}
    }
}
$apiResults|ConvertTo-Json -Depth 30|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'public-api.json') -Encoding utf8
$comparison=foreach($local in $apiResults|Where-Object origin -EQ 'local'){
    $published=$apiResults|Where-Object {$_.origin -eq 'published' -and $_.symbol -eq $local.symbol}
    if($published){$la=@($local.records|Where-Object name -EQ 'publicApi'|ForEach-Object {$_.detail.api});$pa=@($published.records|Where-Object name -EQ 'publicApi'|ForEach-Object {$_.detail.api});
        $localLines=@($la|ForEach-Object {$_.type; $_.properties; $_.methods; $_.constructors})|Sort-Object -Unique
        $publishedLines=@($pa|ForEach-Object {$_.type; $_.properties; $_.methods; $_.constructors})|Sort-Object -Unique
        @{symbol=$local.symbol;differences=@(Compare-Object $publishedLines $localLines)}
    }
}
$comparison|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'public-api-comparison.json') -Encoding utf8
}
$docCases=@()
foreach($doc in @('README.md','DataAccessProvider.MSSQL\README.md','DataAccessProvider.MySql\README.md','DataAccessProvider.Postgres\README.md','DataAccessProvider.MongoDB\README.md','DataAccessProvider.Core\README.md')){
    $content=Get-Content -LiteralPath (Join-Path $RepoRoot $doc) -Raw
    $blocks=[regex]::Matches($content,'(?ms)^```[^\r\n]*\r?\n(.*?)^```[ \t]*\r?$')
    foreach($block in $blocks){$code=$block.Groups[1].Value
        if($code -match 'UseDataAccessProvider' -and $code -match 'services\.AddDataAccessProvider' -and $code -notmatch 'public class'){
            $name=($doc -replace '[\\\.]','-')+'-registration';$symbol=if($doc -match 'MySql'){'MYSQL'}elseif($doc -match 'Postgres'){'POSTGRES'}elseif($doc -match 'MongoDB'){'MONGO'}else{'MSSQL'}
            $case=Join-Path $ScratchRoot "docs\$name";New-Item -ItemType Directory -Force -Path $case|Out-Null
            Copy-Item -LiteralPath (Join-Path $ScratchRoot "api\local-$symbol\Consumer.csproj") -Destination $case -Force
            Copy-Item -LiteralPath (Join-Path $ScratchRoot "api\local-$symbol\NuGet.Config") -Destination $case -Force
            $ns=if($symbol -eq 'MYSQL'){'DataAccessProvider.MySql'}elseif($symbol -eq 'POSTGRES'){'DataAccessProvider.Postgres'}elseif($symbol -eq 'MONGO'){'DataAccessProvider.MongoDB'}else{'DataAccessProvider.MSSQL'}
            $preamble="using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensions.Configuration; using DataAccessProvider.Core.Extensions; using $ns;`nvar services=new ServiceCollection();var configuration=new ConfigurationBuilder().Build();var serviceProvider=services.BuildServiceProvider();`n"
            ($preamble+$code)|Set-Content -LiteralPath (Join-Path $case 'Program.cs') -Encoding utf8
            $project=Join-Path $case 'Consumer.csproj';$null=Run-Captured "docs-$name-restore" @('restore',$project,'--configfile',(Join-Path $case 'NuGet.Config'),'--packages',(Join-Path $ScratchRoot 'local-consumer-cache'),'--verbosity','minimal')
            $result=Run-Captured "docs-$name-build" @('build',$project,'--no-restore','--verbosity','minimal')
            $code|Set-Content -LiteralPath (Join-Path $PSScriptRoot "docs-$name-excerpt.cs.txt") -Encoding utf8
            $docCases+=@{source=$doc;case=$name;build=$result.code}
        }
    }
}
$docCases|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'documentation-checks.json') -Encoding utf8
Write-Output 'API and documentation checks complete'
