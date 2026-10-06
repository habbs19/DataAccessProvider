using DataAccessProvider.Core;
using DataAccessProvider.Snowflake;

// Compile as a one-package consumer. Run only with credentials for a disposable test account.
var connection=Environment.GetEnvironmentVariable("DAP_SNOWFLAKE_TEST_CONNECTION");
if(string.IsNullOrWhiteSpace(connection)) { Console.WriteLine("blocked: no disposable Snowflake account configured"); return 2; }
await using var database=new SnowflakeClient(connection);
if(!(await database.CheckHealthAsync()).Healthy) throw new InvalidOperationException("Test account health failed");
var table="DAP_FIXTURE_"+Guid.NewGuid().ToString("N").ToUpperInvariant();
try
{
    await database.ExecuteAsync(DatabaseCommand.Text($"CREATE TABLE {table} (ID INT)"));
    await database.ExecuteInTransactionAsync(async(tx,ct)=>{await tx.ExecuteAsync(DatabaseCommand.Text($"INSERT INTO {table} VALUES (1)"),ct);});
    if((await database.ScalarAsync<long>(DatabaseCommand.Text($"SELECT COUNT(*) FROM {table}"))).Value!=1)throw new Exception("Commit assertion failed");
    try {await database.ExecuteInTransactionAsync(async(tx,ct)=>{await tx.ExecuteAsync(DatabaseCommand.Text($"INSERT INTO {table} VALUES (2)"),ct);throw new ApplicationException();});}catch(ApplicationException){}
    if((await database.ScalarAsync<long>(DatabaseCommand.Text($"SELECT COUNT(*) FROM {table}"))).Value!=1)throw new Exception("Rollback assertion failed");
    using var cancellation=new CancellationTokenSource();cancellation.Cancel();
    try {await database.ScalarAsync<int>(DatabaseCommand.Text("SELECT 1"),cancellation.Token);throw new Exception("Cancellation assertion failed");}catch(OperationCanceledException){}
    Console.WriteLine("Snowflake account health, commit, rollback and pre-cancellation passed");return 0;
}
finally {await database.ExecuteAsync(DatabaseCommand.Text($"DROP TABLE IF EXISTS {table}"));}
