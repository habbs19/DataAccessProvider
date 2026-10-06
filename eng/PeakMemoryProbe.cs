using System.Diagnostics;
using System.Text.Json;
using DataAccessProvider.Core;
using DataAccessProvider.Postgres;

if(args.Length!=1 || !int.TryParse(args[0],out var port) || port<1024 || port>65535)throw new ArgumentException("Owned fixture loopback port required");
await using var database=new PostgreSqlClient($"Host=127.0.0.1;Port={port};Database=dap_fixture;Username=fixture;Password=DapFixture_14!Database;Timeout=2");
var fields=string.Join(",",Enumerable.Range(1,8).Select(i=>$"repeat('x',500) AS \"C{i}\""));
var command=DatabaseCommand.Text($"SELECT i AS \"Id\",{fields} FROM generate_series(1,100000) i").WithTimeout(TimeSpan.FromMinutes(3));
// Memory, not throughput: input resides on the disposable server and is read through the actual driver.
async Task<(long managed,long working)> Measure(bool buffered)
{
    GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
    var baseline=GC.GetTotalMemory(true);using var process=Process.GetCurrentProcess();process.Refresh();var baselineWorking=process.WorkingSet64;
    long managedPeak=0,workingPeak=0;
    void Sample(){var delta=GC.GetTotalMemory(false)-baseline;managedPeak=Math.Max(managedPeak,delta);process.Refresh();workingPeak=Math.Max(workingPeak,process.WorkingSet64-baselineWorking);}
    using var timer=new Timer(_=>{lock(process){Sample();}},null,0,2);
    QueryResult<WideRow>? rows=null;var count=0;
    if(buffered){rows=await database.QueryAsync<WideRow>(command);count=rows.Rows.Count;}
    else{await foreach(var row in database.StreamAsync<WideRow>(command)){count++;}}
    await timer.DisposeAsync();Sample();GC.KeepAlive(rows);
    if(count!=100000)throw new Exception("Incomplete benchmark read");return(managedPeak,workingPeak);
}
await database.QueryAsync<WideRow>(DatabaseCommand.Text($"SELECT 1 AS \"Id\",{fields}")); // warm mapper and driver
var streamed=await Measure(false);var buffered=await Measure(true);
var passed=streamed.managed<=buffered.managed/2;
Console.WriteLine(JsonSerializer.Serialize(new {rows=100000,columns=9,textCharsPerColumn=500,streamManagedPeakIncrease=streamed.managed,bufferManagedPeakIncrease=buffered.managed,streamWorkingSetPeakIncrease=streamed.working,bufferWorkingSetPeakIncrease=buffered.working,status=passed?"passed":"failed",gate="at least 50% reduction in sampled peak managed memory above baseline"},new JsonSerializerOptions{WriteIndented=true}));
return passed?0:1;
public sealed class WideRow {public int Id{get;set;} public string? C1{get;set;} public string? C2{get;set;} public string? C3{get;set;} public string? C4{get;set;} public string? C5{get;set;} public string? C6{get;set;} public string? C7{get;set;} public string? C8{get;set;}}
