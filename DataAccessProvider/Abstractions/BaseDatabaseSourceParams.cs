using DataAccessProvider.Abstractions;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.DataSource.Params;

interface IBaseDatabaseSourceParams<TParameter> where TParameter : DbParameter
{
    public string Query { get; set; }
    public List<TParameter>? Parameters { get; set; } 
    public CommandType CommandType { get; set; }
    public int Timeout { get; set; }
    public int AffectedRows { get; set; }
}
public abstract class BaseDatabaseSourceParams<TParameter> : BaseDataSourceParams, IBaseDatabaseSourceParams<TParameter>
    where TParameter : DbParameter
{
    public string Query { get; set; } = string.Empty;
    public List<TParameter>? Parameters { get; set; } = new List<TParameter>();
    public CommandType CommandType { get; set; }
    public int Timeout { get; set; }
    public int AffectedRows { get; set; }
}


public abstract class BaseDatabaseSourceParams<TParameter,TValue> : BaseDataSourceParams<TValue>,IBaseDatabaseSourceParams<TParameter>
    where TParameter : DbParameter
    where TValue : class
{
    public string Query { get; set; } = string.Empty;
    public List<TParameter>? Parameters { get; set; }
    public CommandType CommandType { get; set; }
    public int Timeout { get; set; }
    public int AffectedRows { get; set; }
}
