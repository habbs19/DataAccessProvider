using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Resilience;
using DataAccessProvider.Core.Types;
using DataAccessProvider.MSSQL;
using Microsoft.Extensions.DependencyInjection;

var results = new List<object>();
void Record(string name, object detail) => results.Add(new { name, detail });
var services = new ServiceCollection();
services.AddDataAccessProviderMSSQL("Server=127.0.0.1,1;Integrated Security=true");
services.AddScoped<DisposableJsonSource>();
using (var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }))
{
    var factory = provider.GetRequiredService<IDataSourceFactory>();
    factory.RegisterDataSource<JsonFileSourceParams, DisposableJsonSource>();
    var returned = (DisposableJsonSource)factory.CreateDataSource(new JsonFileSourceParams());
    Record("factoryReturnsDisposedScopedService", returned.Disposed);
    factory.RegisterDataSource<FamilyA.SameParams, DisposableJsonSource>();
    factory.RegisterDataSource<FamilyB.SameParams, StaticCodeSource>();
    services.AddScoped<StaticCodeSource>();
    // Inspect the registration target; no operation is executed with incompatible parameters.
    Record("sameNameRegistrationCollision", factory.GetRegisteredDataSources()[nameof(FamilyA.SameParams)].FullName!);
    factory.GetRegisteredDataSources().Clear();
    Record("mappingDictionaryIsExternallyMutable", factory.GetRegisteredDataSources().Count == 0);
}

var policy = new CountingPolicy();
var generic = new ProbeSource(policy);
await generic.ExecuteNonQueryAsync(new ProbeParams());
Record("genericSourcePolicyCalls", policy.Calls);
Record("genericResourcesDisposed", new { connection = generic.Connection.Disposed, command = generic.Command.Disposed });

var direct = new DirectProbeSource(new BasicResiliencePolicy(2, TimeSpan.FromSeconds(2)));
direct.Command.Fail = true;
try { await direct.ExecuteNonQueryAsync(new ProbeParams()); } catch(Exception ex) { Record("retryConnectionReuse", new { error = ex.GetType().Name, direct.Connection.Opens, direct.Command.Executions }); }

var exceptionAttempts = 0;
try { await new BasicResiliencePolicy(2, TimeSpan.FromSeconds(2)).ExecuteAsync<int>(_ => { exceptionAttempts++; throw new ArgumentException("permanent input error"); }); } catch (ArgumentException) { }
Record("permanentFailureAttempts", exceptionAttempts);
var timeoutWatch=Stopwatch.StartNew();
var timeoutResult=await new BasicResiliencePolicy(0,TimeSpan.FromMilliseconds(5)).ExecuteAsync(async _=>{await Task.Delay(50);return "completed";});
Record("timeoutIsCooperativeOnly",new {timeoutResult,elapsedMs=timeoutWatch.Elapsed.TotalMilliseconds});

var typed = new MSSQLSourceParams<Row> { Query = "SELECT 1", CommandType = CommandType.Text };
try { await ((IDataSource)new MSSQLSource("Server=127.0.0.1,1")).ExecuteReaderAsync<Row, MSSQLSourceParams<Row>>(typed); }
catch(Exception ex) { Record("explicitTypedOverloadFailureBeforeDatabase", new { type=ex.GetType().Name, ex.Message }); }

var output = new DataAccessParameter { ParameterName="@out", Direction=DataAccessParameterDirection.Output, DbType=DataAccessDbType.Int32, Value=0 };
generic.Command.Fail=false;
var outputParams=new ProbeParams(); outputParams.Parameters!.Add(output);
await generic.ExecuteNonQueryAsync(outputParams);
Record("outputParameterCopyBack", output.Value);

var stale = new TypedProbeParams<Row>(); stale.SetValue(new Row { Id=99 });
await generic.ExecuteReaderAsync<Row>(stale);
Record("emptyTypedReadRetainsOldRows", stale.Value!.Select(x=>x.Id).ToArray());
var row = new Dictionary<string, object> { ["Id"]="not-an-integer" }.MapTo<Row>();
Record("dictionaryMapperSilentlyDefaultsBadValue", row.Id);
Record("ordinaryCancellationOverloads", typeof(IDataSource).GetMethods().Count(m=>m.GetParameters().Any(p=>p.ParameterType==typeof(CancellationToken))));
try {await new StaticCodeSource().ExecuteReaderAsync<Row>(new StaticCodeParams<Row>());}
catch(Exception ex){Record("staticTypedReadFailure",ex.GetType().Name);}
Record("defaultCommandSettings", new { new MSSQLSourceParams().CommandType, new MSSQLSourceParams().Timeout });

var edge=new DataTable();
edge.Columns.Add("id",typeof(long));edge.Columns.Add("Optional",typeof(int));edge.Columns.Add("Status",typeof(string));edge.Columns.Add("Token",typeof(string));edge.Columns.Add("Enabled",typeof(string));
edge.Rows.Add(7L,DBNull.Value,"Active","a72d79a9-c499-453b-977d-67e6cf4c1b91","true");
generic.Command.Table=edge;
var mapped=await generic.ExecuteReaderAsync<EdgeRow>(new TypedProbeParams<EdgeRow>());
Record("mappingCaseNumericNullableEnumGuidBool",mapped.Value!.First());
var nullData=new DataTable();nullData.Columns.Add("Id",typeof(int));nullData.Rows.Add(DBNull.Value);generic.Command.Table=nullData;
Record("mappingNullNonNullableRetainsInitializer",(await generic.ExecuteReaderAsync<RowWithDefault>(new TypedProbeParams<RowWithDefault>())).Value!.First().Id);
var dates=new DataTable();dates.Columns.Add("Day",typeof(DateTime));dates.Rows.Add(new DateTime(2026,10,5));generic.Command.Table=dates;
try{await generic.ExecuteReaderAsync<DateRow>(new TypedProbeParams<DateRow>());}catch(Exception ex){Record("mappingDateTimeToDateOnly",ex.GetType().Name);}
generic.Command=new FakeCommand {Table=edge};generic.Connection=new FakeConnection();
try{await generic.ExecuteReaderAsync<IndexerRow>(new TypedProbeParams<IndexerRow>());}catch(Exception ex){Record("mappingIndexerAndFailureCleanup",new {error=ex.GetType().Name,connectionDisposed=generic.Connection.Disposed,commandDisposed=generic.Command.Disposed,readerClosed=generic.Command.LastReader?.IsClosed});}

var setupFailure=new ProbeSource(new CountingPolicy()){ThrowParameterCreation=true};
var badCommand=new ProbeParams();badCommand.Parameters!.Add(new DataAccessParameter());
try{await setupFailure.Transaction(async(tx,ct)=>await tx.ExecuteNonQueryAsync(badCommand,ct));}
catch(ArgumentException){Record("transactionParameterSetupFailureCleanup",new {connectionDisposed=setupFailure.Connection.Disposed,transactionDisposed=setupFailure.Connection.Transaction!.Disposed,commandDisposed=setupFailure.Command.Disposed});}
var simulatedWrites=0;
try{await new BasicResiliencePolicy(2,TimeSpan.FromSeconds(2)).ExecuteAsync<int>(_=>{simulatedWrites++;throw new IOException("simulated acknowledgement loss after write");});}catch(IOException){}
Record("retryRepeatedWriteSimulation",simulatedWrites);

var ownedFile=Path.Combine(Path.GetTempPath(),"dap-review-"+Guid.NewGuid().ToString("N")+".json");
try {
    var files=new JsonFileSource();
    try {await files.ExecuteNonQueryAsync(new JsonFileSourceParams {FilePath=ownedFile,Content="{}"});}
    catch(Exception ex){Record("fileWriterCannotCreateNewFile",ex.GetType().Name);}
    await File.WriteAllTextAsync(ownedFile,"{}");
    var p=new JsonFileSourceParams {FilePath=ownedFile,Content="é",Encoding=new System.Text.UTF8Encoding(false)};
    await files.ExecuteNonQueryAsync(p);
    Record("fileWriteCharacterByteMismatch",new {reported=p.Value,actualFileBytes=new FileInfo(ownedFile).Length});
} finally {if(File.Exists(ownedFile))File.Delete(ownedFile);}

// Local microbenchmark: repeatedly materialize one-row BCL DataTable readers, no server or network.
var table=new DataTable(); table.Columns.Add("Id",typeof(int)); table.Rows.Add(7);
generic.Command.Table=table;
await generic.ExecuteReaderAsync<Row>(new TypedProbeParams<Row>());
foreach(var rows in new[] { 1, 1000, 10000 })
{
    var data = table.Clone(); for(var i=0;i<rows;i++) data.Rows.Add(i);
    generic.Command.Table=data;
    var before=GC.GetTotalAllocatedBytes(true); var timer=Stopwatch.StartNew();
    for(var i=0;i<5;i++) await generic.ExecuteReaderAsync<Row>(new TypedProbeParams<Row>());
    timer.Stop();
    Record("mappingMeasurement", new { rows, repetitions=5, elapsedMs=timer.Elapsed.TotalMilliseconds, allocatedBytes=GC.GetTotalAllocatedBytes(true)-before });
}
Console.WriteLine(JsonSerializer.Serialize(results,new JsonSerializerOptions { WriteIndented=true }));

public sealed class Row { public int Id { get; set; } }
public enum Status {Inactive,Active}
public sealed class EdgeRow {public int Id {get;set;} public int? Optional {get;set;} public Status Status {get;set;} public Guid Token {get;set;} public bool Enabled {get;set;} }
public sealed class RowWithDefault {public int Id {get;set;}=99;}
public sealed class DateRow {public DateOnly Day {get;set;} }
public sealed class IndexerRow {public string this[int index] {get=>"";set{}} }
namespace FamilyA { public sealed class SameParams : BaseDataSourceParams {} }
namespace FamilyB { public sealed class SameParams : BaseDataSourceParams {} }
public sealed class DisposableJsonSource : JsonFileSource, IDisposable { public bool Disposed {get;private set;} public void Dispose()=>Disposed=true; }
public sealed class ProbeParams : BaseDatabaseSourceParams { }
public sealed class TypedProbeParams<T> : BaseDatabaseSourceParams<T> where T:class { }
public sealed class CountingPolicy : IResiliencePolicy { public int Calls; public Task<T> ExecuteAsync<T>(Func<CancellationToken,Task<T>> action,CancellationToken cancellationToken=default) { Calls++; return action(cancellationToken); } }
public sealed class ProbeSource : BaseDatabaseSource<ProbeParams>
{
    public FakeConnection Connection=new(); public FakeCommand Command=new();
    public bool ThrowParameterCreation;
    public ProbeSource(IResiliencePolicy policy):base("fake",policy){ }
    public override DbConnection GetConnection(){Connection.Reset();return Connection;}
    public override DbCommand GetCommand(string query,DbConnection connection)=>Command;
    protected override DbParameter CreateDbParameter(DbCommand command,DataAccessParameter parameter){if(ThrowParameterCreation)throw new ArgumentException("invalid parameter fixture");return new FakeParameter { ParameterName=parameter.ParameterName,Value=parameter.Value,Direction=parameter.Direction == DataAccessParameterDirection.Input ? ParameterDirection.Input : ParameterDirection.Output };}
    public Task Transaction(Func<IDatabaseTransaction,CancellationToken,Task> action)=>ExecuteInTransactionCoreAsync(action,null,CancellationToken.None);
}
public sealed class DirectProbeSource : BaseDatabaseSource
{
    public FakeConnection Connection=new(); public FakeCommand Command=new();
    public DirectProbeSource(IResiliencePolicy policy):base("fake",policy){ }
    public override DbConnection GetConnection()=>Connection;
    public override DbCommand GetCommand(string query,DbConnection connection)=>Command;
    protected override DbParameter CreateDbParameter(DbCommand command,DataAccessParameter parameter)=>new FakeParameter();
}
public sealed class FakeConnection : DbConnection
{
    public bool Disposed; public int Opens; private ConnectionState _state;
    public FakeTransaction? Transaction;
    public void Reset(){Disposed=false;_state=ConnectionState.Closed;}
    public override string ConnectionString { get;set; }="";
    public override string Database=>"fake"; public override string DataSource=>"fake"; public override string ServerVersion=>"1"; public override ConnectionState State=>_state;
    public override void Open(){Opens++;if(_state==ConnectionState.Open)throw new InvalidOperationException("already open");_state=ConnectionState.Open;}
    public override void Close()=>_state=ConnectionState.Closed;
    public override void ChangeDatabase(string databaseName){ }
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)=>Transaction=new FakeTransaction(this);
    protected override DbCommand CreateDbCommand()=>new FakeCommand();
    protected override void Dispose(bool disposing){Disposed=true;Close();base.Dispose(disposing);}
}
public sealed class FakeTransaction(FakeConnection connection) : DbTransaction {
 public bool Disposed; public override IsolationLevel IsolationLevel=>IsolationLevel.ReadCommitted;
 protected override DbConnection DbConnection=>connection;public override void Commit(){}public override void Rollback(){}
 protected override void Dispose(bool disposing){Disposed=true;base.Dispose(disposing);}
}
public sealed class FakeCommand : DbCommand
{
    public bool Disposed; public bool Fail; public int Executions; public DataTable Table=new();
    public DbDataReader? LastReader;
    private readonly FakeParameters _parameters=new();
    public override string CommandText {get;set;}=""; public override int CommandTimeout {get;set;}
    public override CommandType CommandType {get;set;} public override bool DesignTimeVisible {get;set;} public override UpdateRowSource UpdatedRowSource {get;set;}
    protected override DbConnection? DbConnection {get;set;} protected override DbTransaction? DbTransaction {get;set;}
    protected override DbParameterCollection DbParameterCollection=>_parameters;
    public override void Cancel(){} public override void Prepare(){}
    public override int ExecuteNonQuery(){Executions++; if(Fail)throw new InvalidOperationException("execution failed");foreach(DbParameter p in _parameters)if(p.Direction!=ParameterDirection.Input)p.Value=42;return 1;}
    public override object ExecuteScalar()=>7;
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)=>LastReader=Table.CreateDataReader();
    protected override DbParameter CreateDbParameter()=>new FakeParameter();
    protected override void Dispose(bool disposing){Disposed=true;base.Dispose(disposing);}
}
public sealed class FakeParameter : DbParameter
{
    public override DbType DbType{get;set;} public override ParameterDirection Direction{get;set;}
    public override bool IsNullable{get;set;} public override string ParameterName{get;set;}="";public override string SourceColumn{get;set;}="";
    public override object? Value{get;set;}public override bool SourceColumnNullMapping{get;set;}public override int Size{get;set;}public override void ResetDbType(){}
}
public sealed class FakeParameters : DbParameterCollection
{
    private readonly List<DbParameter> _items=new();
    public override int Count=>_items.Count;public override object SyncRoot=>this;
    public override int Add(object value){_items.Add((DbParameter)value);return _items.Count-1;}
    public override void AddRange(Array values){foreach(var value in values)Add(value!);}
    public override void Clear()=>_items.Clear(); public override bool Contains(object value)=>_items.Contains((DbParameter)value);
    public override bool Contains(string value)=>_items.Any(p=>p.ParameterName==value);public override void CopyTo(Array array,int index)=>((ICollection)_items).CopyTo(array,index);
    public override IEnumerator GetEnumerator()=>_items.GetEnumerator();public override int IndexOf(object value)=>_items.IndexOf((DbParameter)value);
    public override int IndexOf(string value)=>_items.FindIndex(p=>p.ParameterName==value);public override void Insert(int index,object value)=>_items.Insert(index,(DbParameter)value);
    public override void Remove(object value)=>_items.Remove((DbParameter)value);public override void RemoveAt(int index)=>_items.RemoveAt(index);public override void RemoveAt(string name)=>RemoveAt(IndexOf(name));
    protected override DbParameter GetParameter(int index)=>_items[index];protected override DbParameter GetParameter(string name)=>_items[IndexOf(name)];
    protected override void SetParameter(int index,DbParameter value)=>_items[index]=value;protected override void SetParameter(string name,DbParameter value)=>_items[IndexOf(name)]=value;
}
