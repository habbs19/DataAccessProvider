param([string]$ScratchRoot=(Join-Path $env:TEMP 'DataAccessProvider-review-0fe75f7'),[string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path)
$ErrorActionPreference='Stop'
$docs=@('README.md','DataAccessProvider.Core/README.md','DataAccessProvider.MSSQL/README.md','DataAccessProvider.MySql/README.md','DataAccessProvider.Postgres/README.md','DataAccessProvider.MongoDB/README.md')
$projectText=@'
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><FrameworkReference Include="Microsoft.AspNetCore.App"/><PackageReference Include="DataAccessProvider.MSSQL" Version="1.3.7"/><PackageReference Include="DataAccessProvider.MySql" Version="1.3.4"/><PackageReference Include="DataAccessProvider.PostgreSql" Version="1.3.4"/><PackageReference Include="DataAccessProvider.MongoDB" Version="1.3.3"/></ItemGroup></Project>
'@
$imports=@'
using System.Data;using System.Text.Json;using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;using Microsoft.Extensions.Configuration;
using DataAccessProvider.Core.Abstractions;using DataAccessProvider.Core.Interfaces;using DataAccessProvider.Core.Types;using DataAccessProvider.Core.Extensions;using DataAccessProvider.Core.DataSource.Params;using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.MSSQL;using DataAccessProvider.MySql;using DataAccessProvider.Postgres;using DataAccessProvider.MongoDB;
using Microsoft.Data.SqlClient;using Npgsql;using MongoDB.Bson;using MongoDB.Driver;
'@
$results=@()
foreach($doc in $docs){
    $raw=Get-Content -LiteralPath (Join-Path $RepoRoot $doc) -Raw
    $fences=[regex]::Matches($raw,'(?ms)^```([^\r\n]*)\r?\n(.*?)^```[ \t]*\r?$')
    for($i=0;$i -lt $fences.Count;$i++){
        $lang=$fences[$i].Groups[1].Value.Trim();$code=$fences[$i].Groups[2].Value
        $line=1+([regex]::Matches($raw.Substring(0,$fences[$i].Index),'\n')).Count
        if($lang -notmatch 'csharp|C#'){continue}
        $name=($doc -replace '[\\/\.]','-')+"-fence-$i"
        if($code -match '^1:\s'){$results+=@{source=$doc;line=$line;name=$name;status='not applicable';reason='Displayed result output, not executable C#'};continue}
        $case=Join-Path $ScratchRoot "all-docs\$name";New-Item -ItemType Directory -Force -Path $case|Out-Null
        $project=Join-Path $case 'Example.csproj';$projectText|Set-Content -LiteralPath $project -Encoding utf8
        Copy-Item -LiteralPath (Join-Path $ScratchRoot 'consumers/local-MSSQL/NuGet.Config') -Destination $case -Force
        $code|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$name-excerpt.cs.txt") -Encoding utf8
        # Supply surrounding host objects and illustrative domain classes only.
        # Using directives and the record declaration are moved outside a method,
        # preserving their text and every statement in the fenced example.
        $directiveText=([regex]::Matches($code,'(?m)^using [^;\r\n]+;[ \t]*$')|ForEach-Object Value)-join "`n"
        $body=[regex]::Replace($code,'(?m)^using [^;\r\n]+;[ \t]*$','')
        $recordText=([regex]::Matches($body,'(?m)^public record [^;\r\n]+;')|ForEach-Object Value)-join "`n"
        $body=[regex]::Replace($body,'(?m)^public record [^;\r\n]+;','')
        $family=if($doc -match 'Postgres'){'PostgresSourceParams'}elseif($doc -match 'MySql'){'MySQLSourceParams'}else{'MSSQLSourceParams'}
        $stubs="public sealed class Diary {public int Id {get;set;} public string? Title {get;set;} public DateTime Date {get;set;}}`n"
        if($recordText -notmatch 'record User'){$stubs+='public sealed class User {public int Id {get;set;} public string? Name {get;set;}}' + "`n"}
        if($body -notmatch 'class XmlFileSourceParams'){$stubs+='public class XmlFileSourceParams:BaseDataSourceParams {public string FilePath {get;set;}="";public string RootElement {get;set;}="";public string? XPathQuery {get;set;}public bool IgnoreNamespaces {get;set;}public Dictionary<string,string>? AdditionalAttributes {get;set;}}' + "`n"}
        $stubs+='public class XmlFileSource:JsonFileSource {}' + "`n"
        if($body -match '^public void RegisterDataSource'){$wrapped="public interface ExampleDeclaration {`n$body`n}"}
        elseif($body -match '^public class '){$wrapped=$body}
        else{$wrapped="class ExampleScaffold { IServiceProvider serviceProvider=null!;IServiceCollection services=null!;IConfiguration configuration=null!;IDataSourceProvider dataSourceProvider=null!;CancellationToken cancellationToken=default;$family firstCommand=new();$family secondCommand=new();async Task Run(){`n$body`n} }"}
        ($imports+"`n"+$directiveText+"`n"+$wrapped+"`n"+$recordText+"`n"+$stubs)|Set-Content -LiteralPath (Join-Path $case 'Program.cs') -Encoding utf8
        $restoreOut=& dotnet restore $project --configfile (Join-Path $case 'NuGet.Config') --packages (Join-Path $ScratchRoot 'local-consumer-cache') --verbosity quiet 2>&1|Out-String;$restore=$LASTEXITCODE
        $buildOut=& dotnet build $project --no-restore --verbosity quiet 2>&1|Out-String;$build=$LASTEXITCODE
        "RestoreExitCode: $restore`n$restoreOut`nBuildExitCode: $build`n$buildOut"|Set-Content -LiteralPath (Join-Path $PSScriptRoot "$name-build.txt") -Encoding utf8
        $results+=@{source=$doc;line=$line;name=$name;status=if($build -eq 0){'passed'}else{'failed'};restore=$restore;build=$build;scaffold='Hosted, four provider packages, no execution; not an isolated consumer test'}
        Write-Host "$name build=$build"
    }
}
$results|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'all-documentation-checks.json') -Encoding utf8
