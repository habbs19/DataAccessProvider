param([Parameter(Mandatory)][string]$ApplicationRoot)
$ErrorActionPreference='Stop'
$ApplicationRoot=(Resolve-Path $ApplicationRoot).Path
$pattern='\b(IDataSource(?:Provider|Factory)?|Base(?:Database|Data)SourceParams|BaseDatabaseSource|(?:MSSQL|MySQL|Postgres|Oracle|Snowflake)SourceParams|MongoDBParams|JsonFileSourceParams|StaticCodeParams|DataSourceFactory|DataSourceProvider|UseDataAccessProvider\w+)\b'
$matches=@(Get-ChildItem -LiteralPath $ApplicationRoot -Recurse -Filter '*.cs' | Where-Object {$_.FullName -notmatch '[\\/](bin|obj|review)[\\/]'} | Select-String -Pattern $pattern)
if($matches){$matches|ForEach-Object {Write-Host "$($_.Path):$($_.LineNumber) legacy identifier"};throw 'Legacy execution APIs remain: migrate before adopting 2.0'}
Write-Host 'No listed legacy execution identifiers found; compile and test the application against the new API as well.'
