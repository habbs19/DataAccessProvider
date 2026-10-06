using System.Data;
using System.Diagnostics;
using System.Text.Json;
using DataAccessProvider.Core;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
#if MSSQL
using DataAccessProvider.MSSQL;
using Client = DataAccessProvider.MSSQL.SqlServerClient;
using Source = DataAccessProvider.MSSQL.MSSQLSource;
using Params = DataAccessProvider.MSSQL.MSSQLSourceParams;
using TypedParams = DataAccessProvider.MSSQL.MSSQLSourceParams<Row>;
#elif POSTGRES
using DataAccessProvider.Postgres;
using Client = DataAccessProvider.Postgres.PostgreSqlClient;
using Source = DataAccessProvider.Postgres.PostgresSource;
using Params = DataAccessProvider.Postgres.PostgresSourceParams;
using TypedParams = DataAccessProvider.Postgres.PostgresSourceParams<Row>;
#elif MYSQL
using DataAccessProvider.MySql;
using Client = DataAccessProvider.MySql.MySqlClient;
using Source = DataAccessProvider.MySql.MySQLSource;
using Params = DataAccessProvider.MySql.MySQLSourceParams;
using TypedParams = DataAccessProvider.MySql.MySQLSourceParams<Row>;
#elif ORACLE
using DataAccessProvider.Oracle;
using Client = DataAccessProvider.Oracle.OracleClient;
using Source = DataAccessProvider.Oracle.OracleSource;
using Params = DataAccessProvider.Oracle.OracleSourceParams;
using TypedParams = DataAccessProvider.Oracle.OracleSourceParams<Row>;
#else
using DataAccessProvider.MongoDB;
using MongoDB.Driver;
using MongoDB.Bson;
#endif

if (args.Length != 1 || !int.TryParse(args[0], out var port) || port < 1024 || port > 65535)
    throw new ArgumentException("Expected the loopback port of the owned fixture.");
var results = new List<object>(); var failures = 0;
void Require(bool condition) { if (!condition) throw new InvalidOperationException("Assertion failed"); }
async Task Test(string name, Func<Task> action)
{
    try { await action(); results.Add(new { name, status = "passed" }); }
    catch (Exception ex) { failures++; results.Add(new { name, status = "failed", error = ex.GetType().Name }); }
}
const string password = "DapFixture_14!Database";
#if MONGO
var connection = $"mongodb://127.0.0.1:{port}/dap_fixture?serverSelectionTimeoutMS=2000&replicaSet=dapFixture&directConnection=true";
await using var database = new MongoDocumentClient(connection);
using var source = new MongoDBSource(connection);
for (var i = 0; i < 60 && !(await database.CheckHealthAsync()).Healthy; i++) await Task.Delay(1000);
await Test("document CRUD and counts", async () => {
    Require((await database.InsertManyAsync("probe", new[] { new Row { Id=1, Name="one" }, new Row { Id=2, Name="two" } })).Inserted == 2);
    Require((await database.FindAsync<Row>("probe")).Count == 2);
    Require((await database.FindAsync<Row>("probe", x => x.Id == 2)).Single().Name == "two");
    Require(await database.CountAsync<Row>("probe") == 2);
    Require((await database.SetAsync<Row,string?>("probe", x => x.Id == 2, x => x.Name, "changed")).Modified == 1);
    Require((await database.ReplaceAsync("probe", (Row x) => x.Id == 3, new Row { Id=3 }, upsert:true)).Upserted);
    Require((await database.DeleteAsync<Row>("probe", x => x.Id == 3)).Deleted == 1);
});
await Test("legacy default find and typed filters", async () => {
    var raw = await source.ExecuteReaderAsync(new MongoDBParams { CollectionName="probe" }); Require(raw.Value is not null);
    var typed = new MongoDBParams<Row> { CollectionName="probe", Filter=Builders<Row>.Filter.Eq(x=>x.Id,2) };
    await source.ExecuteReaderAsync<Row,MongoDBParams<Row>>(typed); Require(typed.Value!.Single().Name == "changed");
    typed.Filter=Builders<Row>.Filter.Eq(x=>x.Id,-1); await source.ExecuteReaderAsync<Row>(typed); Require(typed.Value!.Count() == 0);
    var all = await ((IDataSource<MongoDBParams>)source).ExecuteReaderAsync<Row>(new() {CollectionName="probe"}); Require(all.Value!.Count() == 2);
});
await Test("cancel before execution and native cursor cleanup", async () => {
    using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
    try { await database.FindAsync<Row>("probe", cancellationToken:cancellation.Token); throw new Exception(); } catch(OperationCanceledException) { }
    Require(await database.ExecuteNativeAsync(async (db,ct) => { using var cursor=await db.GetCollection<Row>("probe").FindAsync(Builders<Row>.Filter.Empty,cancellationToken:ct); return (await cursor.ToListAsync(ct)).Count; }) == 2);
    Require((await database.CheckHealthAsync()).Healthy);
});
await Test("native MongoDB session commit and abort", async () => {
    await database.ExecuteNativeAsync(async(db,ct)=> {
        using var session=await db.Client.StartSessionAsync(cancellationToken:ct);var collection=db.GetCollection<Row>("probe");
        session.StartTransaction();await collection.InsertOneAsync(session,new Row {Id=4,Name="committed"},cancellationToken:ct);await session.CommitTransactionAsync(ct);
        session.StartTransaction();await collection.InsertOneAsync(session,new Row {Id=5,Name="aborted"},cancellationToken:ct);await session.AbortTransactionAsync(ct);return true;
    });
    Require(await database.CountAsync<Row>("probe",x=>x.Id==4)==1);Require(await database.CountAsync<Row>("probe",x=>x.Id==5)==0);
});
await Test("owned and borrowed MongoDB client disposal",()=> {
    var owned=System.Reflection.DispatchProxy.Create<IMongoClient,DisposalSpy>();var borrowed=System.Reflection.DispatchProxy.Create<IMongoClient,DisposalSpy>();
    var owns=new MongoDBSource(connection,owned,ResourceOwnership.Owned);owns.Dispose();owns.Dispose();
    var borrows=new MongoDBSource(connection,borrowed,ResourceOwnership.Borrowed);borrows.Dispose();
    Require(((DisposalSpy)(object)owned).Count==1);Require(((DisposalSpy)(object)borrowed).Count==0);return Task.CompletedTask;
});
#else
#if MSSQL
var connection = $"Server=127.0.0.1,{port};Database=master;User ID=sa;Password={password};Encrypt=false;Connect Timeout=2";
#elif POSTGRES
var connection = $"Host=127.0.0.1;Port={port};Database=dap_fixture;Username=fixture;Password={password};Timeout=2";
#elif MYSQL
var connection = $"Server=127.0.0.1;Port={port};Database=dap_fixture;User ID=fixture;Password={password};Connection Timeout=2;SslMode=None";
#else
var connection = $"User Id=system;Password={password};Data Source=127.0.0.1:{port}/FREEPDB1;Connection Timeout=2";
#endif
await using var database = new Client(connection); var source = new Source(connection);
for (var i = 0; i < 90 && !(await database.CheckHealthAsync()).Healthy; i++) await Task.Delay(1000);
await Test("health", async () => Require((await database.CheckHealthAsync()).Healthy));
await database.ExecuteAsync(DatabaseCommand.Text("CREATE TABLE probe (Id int NOT NULL PRIMARY KEY, Name varchar(100) NULL)"));
Params Legacy(string sql) => new() { Query=sql, CommandType=CommandType.Text, Timeout=5 };
#if ORACLE
const string insert = "INSERT INTO probe (Id, Name) VALUES (:id, :name)";
const string select = "SELECT Id, Name FROM probe WHERE Id=:id";
const string prefix = "";
#else
const string insert = "INSERT INTO probe (Id, Name) VALUES (@id, @name)";
const string select = "SELECT Id, Name FROM probe WHERE Id=@id";
const string prefix = "@";
#endif
await Test("parameterized write query scalar and native ownership", async () => {
    const string payload="'; DROP TABLE probe; --";
    Require((await database.ExecuteAsync(DatabaseCommand.Text(insert).WithParameter(prefix+"id",DataAccessDbType.Int32,1).WithParameter(prefix+"name",DataAccessDbType.String,payload))).AffectedRows == 1);
    Require((await database.QueryAsync<Row>(DatabaseCommand.Text(select).WithParameter(prefix+"id",DataAccessDbType.Int32,1))).Rows.Single().Name == payload);
    Require((await database.ScalarAsync<long>(DatabaseCommand.Text("SELECT COUNT(*) FROM probe"))).Value == 1);
    Require(await database.ExecuteNativeAsync((conn,tx,ct)=>Task.FromResult(conn.State==ConnectionState.Open && tx is null)));
    await foreach(var row in database.StreamAsync<Row>(DatabaseCommand.Text("SELECT Id, Name FROM probe"))) {Require(row.Id==1);break;}
    Require((await database.CheckHealthAsync()).Healthy);
});
await Test("legacy typed paths and reused empty result", async () => {
    var p = new TypedParams {Query="SELECT Id, Name FROM probe",CommandType=CommandType.Text,Timeout=5};
    await source.ExecuteReaderAsync<Row,TypedParams>(p); Require(p.Value!.Count() == 1);
    Require((await ((IDataSource<Params>)source).ExecuteReaderAsync<Row>(Legacy("SELECT Id, Name FROM probe"))).Value!.Count() == 1);
    p.Query="SELECT Id, Name FROM probe WHERE Id=-1"; await source.ExecuteReaderAsync<Row>(p); Require(p.Value!.Count() == 0);
});
await Test("commit rollback cancellation and escaped session", async () => {
    IDatabaseSession? escaped=null;
    await database.ExecuteInTransactionAsync(async (tx,ct)=> { escaped=tx; await tx.ExecuteAsync(DatabaseCommand.Text("INSERT INTO probe VALUES (2,'committed')"),ct); });
    try { await escaped!.ScalarAsync<int>(DatabaseCommand.Text("SELECT COUNT(*) FROM probe")); throw new Exception(); } catch(InvalidOperationException) { }
    try { await database.ExecuteInTransactionAsync(async(tx,ct)=>{await tx.ExecuteAsync(DatabaseCommand.Text("INSERT INTO probe VALUES (3,'rollback')"),ct);throw new ApplicationException();}); } catch(ApplicationException) { }
    using var cancellation = new CancellationTokenSource();
    try { await database.ExecuteInTransactionAsync(async(tx,ct)=>{await tx.ExecuteAsync(DatabaseCommand.Text("INSERT INTO probe VALUES (4,'cancelled')"),ct);cancellation.Cancel();},cancellationToken:cancellation.Token);throw new Exception(); } catch(OperationCanceledException) { }
    Require((await database.ScalarAsync<long>(DatabaseCommand.Text("SELECT COUNT(*) FROM probe"))).Value == 2);
});
await Test("output parameters after execution and reader disposal", async () => {
#if MSSQL
    await database.ExecuteAsync(DatabaseCommand.Text("CREATE PROCEDURE fixture_output @out int OUTPUT AS BEGIN SET @out=42; SELECT 1 AS Id; RETURN 9; END"));
    var command=DatabaseCommand.StoredProcedure("fixture_output").WithOutput("@out",DataAccessDbType.Int32).WithReturnValue("@return",DataAccessDbType.Int32);
    var query=await database.QueryAsync<Row>(command); Require(Convert.ToInt32(query.Outputs["@out"])==42 && Convert.ToInt32(query.ReturnValue)==9);
    var result=await database.ExecuteAsync(command); Require(Convert.ToInt32(result.Outputs["@out"])==42);
    var legacy=Legacy("fixture_output");legacy.CommandType=CommandType.StoredProcedure;
    legacy.Parameters!.Add(new DataAccessParameter {ParameterName="@out",DbType=DataAccessDbType.Int32,Direction=DataAccessParameterDirection.Output});
    await source.ExecuteReaderAsync(legacy);Require(Convert.ToInt32(legacy.Parameters[0].Value)==42);
#elif POSTGRES
    await database.ExecuteAsync(DatabaseCommand.Text("CREATE PROCEDURE fixture_output(INOUT o integer) LANGUAGE plpgsql AS $$ BEGIN o:=42; END; $$"));
    var command=new DatabaseCommand("fixture_output",CommandType.StoredProcedure,parameters:[new("o",DataAccessDbType.Int32,0,DataAccessParameterDirection.InputOutput)]);
    var query=await database.QueryAsync<Row>(command);Require(Convert.ToInt32(query.Outputs["o"])==42);
#elif MYSQL
    await database.ExecuteAsync(DatabaseCommand.Text("CREATE PROCEDURE fixture_output(OUT o int) BEGIN SET o=42; SELECT 1 AS Id; END"));
    var command=DatabaseCommand.StoredProcedure("fixture_output").WithOutput("o",DataAccessDbType.Int32);
    var query=await database.QueryAsync<Row>(command);Require(Convert.ToInt32(query.Outputs["o"])==42);
#else
    var command=DatabaseCommand.Text("BEGIN :out := 42; END;").WithOutput("out",DataAccessDbType.Int32);
    Require(Convert.ToInt32((await database.ExecuteAsync(command)).Outputs["out"])==42);
    await database.ExecuteAsync(DatabaseCommand.Text("CREATE OR REPLACE PROCEDURE fixture_output(o OUT NUMBER) AS BEGIN o:=42; END;"));
    Require(Convert.ToInt32((await database.ExecuteAsync(DatabaseCommand.StoredProcedure("fixture_output").WithOutput("o",DataAccessDbType.Int32))).Outputs["o"])==42);
    var legacy=Legacy("BEGIN :out := 42; END;");legacy.Parameters!.Add(new DataAccessParameter {ParameterName="out",DbType=DataAccessDbType.Int32,Direction=DataAccessParameterDirection.Output});
    await source.ExecuteNonQueryAsync(legacy);Require(Convert.ToInt32(legacy.Parameters[0].Value)==42);
#endif
});
#if !ORACLE
await Test("ordered multiple sets and strict duplicate rejection", async () => {
    Require((await database.QueryMultipleAsync(DatabaseCommand.Text("SELECT 1 AS Id; SELECT 2 AS Id"))).ResultSets.Count==2);
    try { await database.QueryMultipleAsync(DatabaseCommand.Text("SELECT 1 AS Id, 2 AS Id"));throw new Exception(); } catch(InvalidOperationException) { }
});
#endif
await Test("in-flight cancellation and healthy reuse", async () => {
    using var cancellation = new CancellationTokenSource(200);var timer=Stopwatch.StartNew();
#if MSSQL
    var slow=DatabaseCommand.Text("WAITFOR DELAY '00:00:10'; SELECT 1");
#elif POSTGRES
    var slow=DatabaseCommand.Text("SELECT pg_sleep(10)");
#elif ORACLE
    var slow=DatabaseCommand.Text("BEGIN DBMS_SESSION.SLEEP(10); END;");
#else
    var slow=DatabaseCommand.Text("SELECT SLEEP(10)");
#endif
    string outcome="completed";
    try {
#if ORACLE
        await database.ExecuteAsync(slow,cancellation.Token);
#else
        await database.ScalarAsync<object>(slow,cancellation.Token);
#endif
        throw new Exception();
    }
    catch(OperationCanceledException) { outcome="OperationCanceledException"; }
    catch(System.Data.Common.DbException ex) when(cancellation.IsCancellationRequested) { outcome=ex.GetType().Name; }
    timer.Stop();var healthy=(await database.CheckHealthAsync()).Healthy;
    results.Add(new {name="cancellation timing observation",status="observed",elapsedMs=timer.Elapsed.TotalMilliseconds,outcome,cancellationRequested=cancellation.IsCancellationRequested,healthyAfter=healthy,interruptedWithinSixSeconds=timer.Elapsed<TimeSpan.FromSeconds(6)});
#if !ORACLE
    Require(timer.Elapsed < TimeSpan.FromSeconds(6));
#endif
    Require(cancellation.IsCancellationRequested);Require(healthy);
});
#endif
Console.WriteLine(JsonSerializer.Serialize(results,new JsonSerializerOptions {WriteIndented=true}));
return failures == 0 ? 0 : 1;
public sealed class Row { public int Id { get; set; } public string? Name { get; set; } }
#if MONGO
public class DisposalSpy : System.Reflection.DispatchProxy {
    public int Count {get;private set;}
    protected override object? Invoke(System.Reflection.MethodInfo? method,object?[]? arguments) {
        if(method?.Name=="Dispose"){Count++;return null;}throw new NotSupportedException();
    }
}
#endif
