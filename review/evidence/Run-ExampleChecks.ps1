param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'),[string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path)
$ErrorActionPreference='Stop'
function Run-Captured([string]$Name,[string[]]$Arguments){
    $out=& dotnet @Arguments 2>&1|Out-String;$code=$LASTEXITCODE
    "dotnet $($Arguments -join ' ')`nExitCode: $code`n$out"|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$Name.txt") -Encoding utf8
    return $code
}
function Blocks([string]$Doc){
    $text=Get-Content -LiteralPath (Join-Path $RepoRoot $Doc) -Raw
    return @([regex]::Matches($text,'(?ms)^```[^\r\n]*\r?\n(.*?)^```[ \t]*\r?$')|ForEach-Object {$_.Groups[1].Value})
}
$cases=@()
$pgBlocks=Blocks 'DataAccessProvider.Postgres/README.md'
$pgQuery=$pgBlocks|Where-Object {$_ -match 'List<NpgsqlParameter>'}|Select-Object -First 1
$pgTyped=$pgBlocks|Where-Object {$_ -match 'public record User'}|Select-Object -First 1
$root=Blocks 'README.md'|Where-Object {$_ -match 'services.AddDataAccessProvider\(configuration\)'}|Select-Object -First 1
$core=Blocks 'DataAccessProvider.Core/README.md'|Where-Object {$_ -match 'services.AddDataAccessProvider\(configuration\)'}|Select-Object -First 1
$cases+=@{name='postgres-query-verbatim';symbol='POSTGRES';code="using Npgsql; using DataAccessProvider.Postgres; using DataAccessProvider.Core.Interfaces; using Microsoft.Extensions.DependencyInjection;`nclass Example { async Task Run(IServiceProvider serviceProvider) {`n$pgQuery`n} }"}
$cases+=@{name='postgres-typed-verbatim';symbol='POSTGRES';code="using DataAccessProvider.Postgres; using DataAccessProvider.Core.Interfaces;`nclass Example { IDataSourceProvider dataSourceProvider=null!; async Task Run() {`n"+($pgTyped -replace 'public record User\(int Id, string Name\);','')+"`n} }`npublic record User(int Id,string Name);"}
foreach($item in @(@{name='root-registration-verbatim';code=$root},@{name='core-registration-verbatim';code=$core})){
    $cases+=@{name=$item.name;symbol='MSSQL';code="using DataAccessProvider.Core.Extensions; using Microsoft.Extensions.DependencyInjection; using Microsoft.Extensions.Configuration;`nclass Example { void Run(IServiceCollection services,IConfiguration configuration) {`n$($item.code)`n} }"}
}
foreach($symbol in @('MSSQL','MYSQL','POSTGRES','MONGO')){
    $cases+=@{name="hosted-registration-$symbol";symbol=$symbol;code=(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'HostedRegistrationProbe.cs') -Raw);run=$true}
}
$corrected=@'
using System.Data;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
using DataAccessProvider.Postgres;
using Microsoft.Extensions.DependencyInjection;
var services=new ServiceCollection();
services.AddDataAccessProviderPostgres("Host=localhost;Port=1;Database=unused;Username=unused");
using var root=services.BuildServiceProvider();
root.UseDataAccessProviderPostgres();
using var scope=root.CreateScope();
var source=scope.ServiceProvider.GetRequiredService<PostgresSource>();
var p=new PostgresSourceParams<User> {
 Query="SELECT id, name FROM users WHERE id=@id",CommandType=CommandType.Text,Timeout=30,
 Parameters=new List<DataAccessParameter>{new(){ParameterName="@id",DbType=DataAccessDbType.Int32,Value=1}}
};
// Compilation only; database operations are in the separate disposable fixture.
if(false) {
 await source.ExecuteReaderAsync<User>(p);
 await source.ExecuteScalarAsync(new PostgresSourceParams {Query="SELECT COUNT(*) FROM users",CommandType=CommandType.Text,Timeout=30});
 await source.ExecuteInTransactionAsync(async(tx,ct)=>await tx.ExecuteNonQueryAsync(new PostgresSourceParams {Query="DELETE FROM users WHERE id=-1",CommandType=CommandType.Text,Timeout=30},ct),cancellationToken:CancellationToken.None);
}
public sealed class User {public int Id {get;set;} public string? Name {get;set;} }
'@
$cases+=@{name='postgres-corrected-query-and-transaction';symbol='POSTGRES';code=$corrected}
$results=@()
foreach($item in $cases){
    $case=Join-Path $ScratchRoot "examples\$($item.name)";New-Item -ItemType Directory -Force -Path $case|Out-Null
    $projectText=Get-Content -LiteralPath (Join-Path $ScratchRoot "api\local-$($item.symbol)\Consumer.csproj") -Raw
    if(!$item.run){$projectText=$projectText.Replace('<OutputType>Exe</OutputType>','<OutputType>Library</OutputType>')}
    if($item.name -eq 'postgres-corrected-query-and-transaction'){$projectText=$projectText.Replace('<OutputType>Library</OutputType>','<OutputType>Exe</OutputType>')}
    $projectText|Set-Content -LiteralPath (Join-Path $case 'Consumer.csproj') -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $ScratchRoot "api\local-$($item.symbol)\NuGet.Config") -Destination $case -Force
    $item.code|Set-Content -LiteralPath (Join-Path $case 'Program.cs') -Encoding utf8
    $item.code|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$($item.name).cs.txt") -Encoding utf8
    $project=Join-Path $case 'Consumer.csproj'
    $restore=Run-Captured "$($item.name)-restore" @('restore',$project,'--configfile',(Join-Path $case 'NuGet.Config'),'--packages',(Join-Path $ScratchRoot 'local-consumer-cache'),'--verbosity','minimal')
    $build=Run-Captured "$($item.name)-build" @('build',$project,'--no-restore','--verbosity','minimal')
    $run=-1
    if($item.run -and $build -eq 0){$run=Run-Captured "$($item.name)-run" @('run','--project',$project,'--no-build','--no-restore')}
    $results+=@{name=$item.name;restore=$restore;build=$build;run=$run;symbol=$item.symbol}
    Write-Host "$($item.name) build=$build run=$run"
}
$results|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'example-checks.json') -Encoding utf8
