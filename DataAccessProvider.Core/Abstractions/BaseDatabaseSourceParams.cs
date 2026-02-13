using System.Data;

namespace DataAccessProvider.Core.Abstractions;

using DataAccessProvider.Core.Types;

interface IBaseDatabaseSourceParams
{
    public string Query { get; set; }
    public List<DataAccessParameter>? Parameters { get; set; }
    public CommandType CommandType { get; set; }
    public int Timeout { get; set; }
    public int AffectedRows { get; set; }
}
public abstract class BaseDatabaseSourceParams : BaseDataSourceParams, IBaseDatabaseSourceParams
{
    public string Query { get; set; } = string.Empty;
    public List<DataAccessParameter>? Parameters { get; set; } = new List<DataAccessParameter>();
    public CommandType CommandType { get; set; } = CommandType.StoredProcedure;
    public int Timeout { get; set; }
    public int AffectedRows { get; set; }
}


public abstract class BaseDatabaseSourceParams<TValue> : BaseDataSourceParams<TValue>, IBaseDatabaseSourceParams
    where TValue : class
{
    public string Query { get; set; } = string.Empty;
    public List<DataAccessParameter>? Parameters { get; set; } = new List<DataAccessParameter>();
    public CommandType CommandType { get; set; } = CommandType.StoredProcedure;
    public int Timeout { get; set; }
    public int AffectedRows { get; set; }
}
