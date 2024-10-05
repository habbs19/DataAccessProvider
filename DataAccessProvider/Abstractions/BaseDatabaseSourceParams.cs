using DataAccessProvider.Abstractions;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.DataSource.Params
{
    public abstract class BaseDatabaseSourceParams<TParameter> : BaseDatabaseSourceParams<object,TParameter> 
        where TParameter : DbParameter { }
    

    public abstract class BaseDatabaseSourceParams<TValue, TParameter> : BaseDataSourceParams<TValue> 
        where TValue : class 
        where TParameter : DbParameter
    {
        public string Query { get; set; } = string.Empty;
        public List<TParameter>? Parameters { get; set; }
        public CommandType CommandType { get; set; }
        public int Timeout { get; set; }
        public int AffectedRows { get; set; }
    }
}
