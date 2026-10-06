using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DataAccessProvider.Core;

// In-memory mapping only. Input tables are allocated outside each measurement.
// The reflection access below is a benchmark hook, not a supported consumer API.
var records = new List<object>();
foreach (var count in new[] {1, 1000, 100000})
foreach (var width in new[] {1, 8})
{
    var table = new DataTable(); table.Columns.Add("Id", typeof(int));
    for (var i=1;i<width;i++) table.Columns.Add("C"+i,typeof(string));
    for(var i=0;i<count;i++) table.Rows.Add(Enumerable.Range(0,width).Select(x=>x==0?(object)i:"value").ToArray());
    var mapperType = typeof(DatabaseCommand).Assembly.GetType("DataAccessProvider.Core.RowMapper`1")!.MakeGenericType(typeof(Row));
    var method=mapperType.GetMethod("Create",BindingFlags.Public|BindingFlags.Static)!;
    (double ms,long allocated,long retained) Measure(bool buffer)
    {
        using var reader=table.CreateDataReader();
        var map=(Func<System.Data.Common.DbDataReader,Row>)method.Invoke(null,[reader,true])!;
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var beforeMemory=GC.GetTotalMemory(true);var before=GC.GetTotalAllocatedBytes(true);var started=Stopwatch.GetTimestamp();
        List<Row>? rows=buffer?new():null;Row? last=null;
        while(reader.Read()) {last=map(reader);rows?.Add(last);}
        var elapsed=Stopwatch.GetElapsedTime(started).TotalMilliseconds;var allocated=GC.GetTotalAllocatedBytes(true)-before;
        var retained=Math.Max(0,GC.GetTotalMemory(true)-beforeMemory);GC.KeepAlive(rows);GC.KeepAlive(last);
        return(elapsed,allocated,retained);
    }
    Measure(true);Measure(false); // warm up member cache and JIT
    foreach(var buffered in new[] {true,false})
    {
        var samples=Enumerable.Range(0,5).Select(_=>Measure(buffered)).OrderBy(x=>x.ms).ToArray();
        records.Add(new {rows=count,columns=width,mode=buffered?"buffered":"streaming prototype",medianMs=samples[2].ms,allocatedBytes=samples[2].allocated,retainedBytes=samples[2].retained,samples=samples.Select(x=>x.ms)});
    }
}
Console.WriteLine(JsonSerializer.Serialize(records,new JsonSerializerOptions {WriteIndented=true}));
public sealed class Row {public int Id{get;set;} public string? C1{get;set;} public string? C2{get;set;} public string? C3{get;set;} public string? C4{get;set;} public string? C5{get;set;} public string? C6{get;set;} public string? C7{get;set;}}
