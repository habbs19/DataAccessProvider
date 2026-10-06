using System.Data;
using System.Text.Json;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
#if MSSQL
using DataAccessProvider.MSSQL;
using Source = DataAccessProvider.MSSQL.MSSQLSource;
using Params = DataAccessProvider.MSSQL.MSSQLSourceParams;
using TypedParams = DataAccessProvider.MSSQL.MSSQLSourceParams<Row>;
#elif POSTGRES
using DataAccessProvider.Postgres;
using Source = DataAccessProvider.Postgres.PostgresSource;
using Params = DataAccessProvider.Postgres.PostgresSourceParams;
using TypedParams = DataAccessProvider.Postgres.PostgresSourceParams<Row>;
#elif MYSQL
using DataAccessProvider.MySql;
using Source = DataAccessProvider.MySql.MySQLSource;
using Params = DataAccessProvider.MySql.MySQLSourceParams;
using TypedParams = DataAccessProvider.MySql.MySQLSourceParams<Row>;
#elif ORACLE
using DataAccessProvider.Oracle;
using Source = DataAccessProvider.Oracle.OracleSource;
using Params = DataAccessProvider.Oracle.OracleSourceParams;
using TypedParams = DataAccessProvider.Oracle.OracleSourceParams<Row>;
#else
using DataAccessProvider.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;
#endif

// Only an ephemeral port is accepted. Hosts, databases, and credentials are fixture constants.
if(args.Length!=1 || !int.TryParse(args[0],out var port) || port<1024 || port>65535)
    throw new ArgumentException("Expected the loopback port of the disposable review container.");
var results=new List<object>();
async Task Test(string name,Func<Task<object?>> action)
{
    try { results.Add(new { name, status="completed", detail=await action() }); }
    catch(Exception ex){ results.Add(new { name,status="exception",error=ex.GetType().Name,message=ex.Message,stack=ex.StackTrace }); }
}
#if MONGO
var source=new MongoDBSource($"mongodb://127.0.0.1:{port}/dap_review?serverSelectionTimeoutMS=2000");
for(var i=0;i<30 && !await source.CheckHealthAsync();i++) await Task.Delay(1000);
await Test("health",async()=>await source.CheckHealthAsync());
await Test("insertTwoDocuments",async()=> (await source.ExecuteNonQueryAsync(new MongoDBParams { CollectionName="probe",OperationType=MongoOperationType.InsertMany,Documents=new() {new BsonDocument { {"_id",1},{"Name","one"} },new BsonDocument { {"_id",2},{"Name","two"} }} })).Value);
await Test("untypedRead",async()=> (await source.ExecuteReaderAsync(new MongoDBParams {CollectionName="probe"})).Value);
await Test("untypedReadWithProjectionAndSort",async()=> (await source.ExecuteReaderAsync(new MongoDBParams {CollectionName="probe",Projection=Builders<BsonDocument>.Projection.Include("_id").Include("Name"),Sort=Builders<BsonDocument>.Sort.Ascending("_id")})).Value);
await Test("scalarCount",async()=> (await source.ExecuteScalarAsync(new MongoDBParams {CollectionName="probe",OperationType=MongoOperationType.Count})).Value);
await Test("typedReadFromUntypedParams",async()=> (await source.ExecuteReaderAsync<Row>(new MongoDBParams {CollectionName="probe"})).Value);
await Test("typedReadWithProjectionAndSort",async()=> (await source.ExecuteReaderAsync<Row>(new MongoDBParams {CollectionName="probe",Projection=Builders<BsonDocument>.Projection.Include("_id").Include("Name"),Sort=Builders<BsonDocument>.Sort.Ascending("_id")})).Value);
await Test("typedReadFromTypedParams",async()=> (await source.ExecuteReaderAsync<Row>(new MongoDBParams<Row> {CollectionName="probe",Filter=Builders<Row>.Filter.Eq(x=>x.Name,"missing")})).Value);
var converter=typeof(MongoDBSource).GetMethod("ConvertToMongoDBParams",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!;
var converted=(MongoDBParams)converter.Invoke(null,new object[] {new MongoDBParams<Row> {CollectionName="probe",Filter=Builders<Row>.Filter.Eq(x=>x.Name,"missing")} })!;
results.Add(new {name="typedFilterDropped",status="completed",detail=converted.Filter is null});
#else
const string password="DapReview_0fe75f7!Database";
#if MSSQL
var connection=$"Server=127.0.0.1,{port};Database=master;User ID=sa;Password={password};Encrypt=false;Connect Timeout=2";
#elif POSTGRES
var connection=$"Host=127.0.0.1;Port={port};Database=dap_review;Username=review;Password={password};Timeout=2";
#elif ORACLE
var connection=$"User Id=system;Password={password};Data Source=127.0.0.1:{port}/FREEPDB1;Connection Timeout=2";
#else
var connection=$"Server=127.0.0.1;Port={port};Database=dap_review;User ID=review;Password={password};Connection Timeout=2;SslMode=None";
#endif
var source=new Source(connection);
for(var i=0;i<60 && !await source.CheckHealthAsync();i++) await Task.Delay(1000);
await Test("health",async()=>await source.CheckHealthAsync());
Params Command(string sql)=>new(){Query=sql,CommandType=CommandType.Text,Timeout=5};
async Task<object?> Scalar(string sql)=>(await source.ExecuteScalarAsync(Command(sql))).Value;
async Task<int> Execute(string sql)=>(await source.ExecuteNonQueryAsync(Command(sql))).AffectedRows;
#if MSSQL
await Execute("CREATE DATABASE dap_review");
source=new Source(connection.Replace("Database=master","Database=dap_review"));
#endif
await Execute("CREATE TABLE probe (Id int NOT NULL PRIMARY KEY, Name varchar(100) NULL)");
await Test("parameterizedInsert",async()=> {
#if ORACLE
    var p=Command("INSERT INTO probe (Id, Name) VALUES (:id, :name)");p.Parameters!.AddParameter("id",DataAccessDbType.Int32,1);p.Parameters.AddParameter("name",DataAccessDbType.String,"'; DROP TABLE probe; --",size:100);
#else
    var p=Command("INSERT INTO probe (Id, Name) VALUES (@id, @name)");p.Parameters!.Add(new DataAccessParameter {ParameterName="@id",DbType=DataAccessDbType.Int32,Value=1});p.Parameters.Add(new DataAccessParameter {ParameterName="@name",DbType=DataAccessDbType.String,Value="'; DROP TABLE probe; --",Size=100});
#endif
    return (await source.ExecuteNonQueryAsync(p)).AffectedRows;
});
#if ORACLE
await Test("parameterizedInsertWithExplicitSizes",async()=> {
    var p=Command("INSERT INTO probe (Id, Name) VALUES (:id, :name)");
    p.Parameters!.AddParameter("id",DataAccessDbType.Int32,1,size:0);
    p.Parameters.AddParameter("name",DataAccessDbType.String,"'; DROP TABLE probe; --",size:100);
    return (await source.ExecuteNonQueryAsync(p)).AffectedRows;
});
#endif
await Test("scalarCount",()=>Scalar("SELECT COUNT(*) FROM probe"));
await Test("untypedRead",async()=> (await source.ExecuteReaderAsync(Command("SELECT Id, Name FROM probe"))).Value);
await Test("typedRead",async()=> (await source.ExecuteReaderAsync<Row>(new TypedParams {Query="SELECT Id, Name FROM probe",CommandType=CommandType.Text,Timeout=5})).Value);
await Test("reusedTypedEmptyRead",async()=> {
    var p=new TypedParams {Query="SELECT Id, Name FROM probe",CommandType=CommandType.Text,Timeout=5};await source.ExecuteReaderAsync<Row>(p);p.Query="SELECT Id, Name FROM probe WHERE Id=-1";await source.ExecuteReaderAsync<Row>(p);return p.Value;
});
await Test("explicitTypedOverload",async()=> (await ((IDataSource)source).ExecuteReaderAsync<Row,TypedParams>(new TypedParams {Query="SELECT Id, Name FROM probe",CommandType=CommandType.Text,Timeout=5})).Value);
await Test("typedSourceInterfaceOverload",async()=> (await ((IDataSource<Params>)source).ExecuteReaderAsync<Row>(Command("SELECT Id, Name FROM probe"))).Value);
#if MSSQL || POSTGRES || MYSQL
await Test("transactionCommit",async()=> {await source.ExecuteInTransactionAsync(async(tx,ct)=>await tx.ExecuteNonQueryAsync(Command("INSERT INTO probe VALUES (2, 'committed')"),ct));return await Scalar("SELECT COUNT(*) FROM probe WHERE Id=2");});
await Test("transactionRollback",async()=> {try{await source.ExecuteInTransactionAsync(async(tx,ct)=>{await tx.ExecuteNonQueryAsync(Command("INSERT INTO probe VALUES (3, 'rolled back')"),ct);throw new InvalidOperationException("fixture rollback");});}catch(InvalidOperationException){}return await Scalar("SELECT COUNT(*) FROM probe WHERE Id=3");});
await Test("transactionEscapedExecutor",async()=> {IDatabaseTransaction? escaped=null;await source.ExecuteInTransactionAsync((tx,ct)=>{escaped=tx;return Task.CompletedTask;});try{await escaped!.ExecuteScalarAsync(Command("SELECT 1"));return "unexpected success";}catch(InvalidOperationException){return "rejected";}});
await Test("transactionCancellation",async()=> {using var cts=new CancellationTokenSource();try{await source.ExecuteInTransactionAsync(async(tx,ct)=>{await tx.ExecuteNonQueryAsync(Command("INSERT INTO probe VALUES (4, 'cancelled')"),ct);cts.Cancel();},cancellationToken:cts.Token);}catch(OperationCanceledException){}return await Scalar("SELECT COUNT(*) FROM probe WHERE Id=4");});
await Test("inFlightTransactionCommandCancellation",async()=> {
    using var cts=new CancellationTokenSource();var timer=System.Diagnostics.Stopwatch.StartNew();string outcome="unexpected completion";string? driverMessage=null;
    try{await source.ExecuteInTransactionAsync(async(tx,ct)=>{
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));
#if MSSQL
        await tx.ExecuteScalarAsync(Command("WAITFOR DELAY '00:00:10'; SELECT 1"),ct);
#elif POSTGRES
        await tx.ExecuteScalarAsync(Command("SELECT pg_sleep(10)"),ct);
#else
        await tx.ExecuteScalarAsync(Command("SELECT SLEEP(10)"),ct);
#endif
    },cancellationToken:cts.Token);}catch(OperationCanceledException){outcome="OperationCanceledException";}
    catch(System.Data.Common.DbException ex) when(cts.IsCancellationRequested){outcome=ex.GetType().Name+" with requested cancellation";driverMessage=ex.Message;}
    timer.Stop();return new {outcome,driverMessage,cancellationRequested=cts.IsCancellationRequested,elapsedMs=timer.Elapsed.TotalMilliseconds,healthAfter=await source.CheckHealthAsync()};
});
#endif
#if ORACLE
await Test("anonymousBlockOutput",async()=> {var p=Command("BEGIN :out := 42; END;");p.Parameters!.AddParameter("out",DataAccessDbType.Int32,0,DataAccessParameterDirection.Output);await source.ExecuteNonQueryAsync(p);return p.Parameters;});
await Test("anonymousBlockOutputWithExplicitSize",async()=> {var p=Command("BEGIN :out := 42; END;");p.Parameters!.AddParameter("out",DataAccessDbType.Int32,0,DataAccessParameterDirection.Output,size:0);await source.ExecuteNonQueryAsync(p);return p.Parameters;});
#endif
#if MSSQL
await Execute("CREATE PROCEDURE review_output @out int OUTPUT AS BEGIN SET @out=42; RETURN 9; END");
await Test("storedProcedureOutput",async()=> {var p=new Params {Query="review_output",CommandType=CommandType.StoredProcedure,Timeout=5};p.AddParameter("@out",DataAccessDbType.Int32,0,DataAccessParameterDirection.Output);p.AddParameter("@return",DataAccessDbType.Int32,0,DataAccessParameterDirection.ReturnValue);await source.ExecuteNonQueryAsync(p);return p.Parameters;});
await Test("documentedSelectDefault",async()=> (await source.ExecuteReaderAsync(new Params {Query="SELECT Id, Name FROM probe",Timeout=5})).Value);
#elif POSTGRES
await Execute("CREATE PROCEDURE review_output(INOUT o integer) LANGUAGE plpgsql AS $$ BEGIN o:=42; END; $$");
await Test("storedProcedureServerOutput",async()=> (await source.ExecuteReaderAsync(Command("CALL review_output(0)"))).Value);
await Test("storedProcedureOutput",async()=> {var p=new Params {Query="review_output",CommandType=CommandType.StoredProcedure,Timeout=5};p.Parameters!.Add(new DataAccessParameter {ParameterName="o",DbType=DataAccessDbType.Int32,Value=0,Direction=DataAccessParameterDirection.InputOutput});await source.ExecuteNonQueryAsync(p);return p.Parameters;});
#elif MYSQL
await Execute("CREATE PROCEDURE review_output(OUT o int) BEGIN SET o=42; END");
await Test("storedProcedureOutput",async()=> {var p=new Params {Query="review_output",CommandType=CommandType.StoredProcedure,Timeout=5};p.AddParameter("o",DataAccessDbType.Int32,0,DataAccessParameterDirection.Output);await source.ExecuteNonQueryAsync(p);return p.Parameters;});
#endif
#if MSSQL || POSTGRES || MYSQL
await Test("multipleResultSets",async()=> (await source.ExecuteReaderAsync(Command("SELECT 1 AS Id; SELECT 2 AS Id"))).Value);
await Test("duplicateColumns",async()=> (await source.ExecuteReaderAsync(Command("SELECT 1 AS Id, 2 AS Id"))).Value);
await Test("mappingErrorDisclosure",async()=> (await source.ExecuteReaderAsync<Row>(new TypedParams {Query="SELECT 'SECRET-SENTINEL' AS Id",CommandType=CommandType.Text,Timeout=5})).Value);
await Test("callerOwnsGetConnection",async()=> {using var owned=source.GetConnection();await owned.OpenAsync();await source.ExecuteScalarAsync(Command("SELECT 1"));return owned.State.ToString();});
#endif
#endif
Console.WriteLine(JsonSerializer.Serialize(results,new JsonSerializerOptions {WriteIndented=true}));
public sealed class Row { public int Id {get;set;} public string? Name {get;set;} }
