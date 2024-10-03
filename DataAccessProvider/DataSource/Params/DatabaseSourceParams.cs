using DataAccessProvider.Abstractions;
using System.Data;

namespace DataAccessProvider.DataSource.Params
{
    public class DatabaseSourceParams<TParameter> : BaseDataSourceParams where TParameter : class
    {
        public string Query { get; set; } = string.Empty;
        public List<TParameter>? Parameters { get; set; }
        public CommandType CommandType { get; set; }
        public int Timeout { get; set; }
        public int AffectedRows { get; set; }
    }

    public class DatabaseParams<TParameter,KValue> : BaseDataSourceParams<KValue> where TParameter : class where KValue : class
    {
        public string Query { get; set; } = string.Empty;
        public List<TParameter>? Parameters { get; set; }
        public CommandType CommandType { get; set; }
        public int Timeout { get; set; }
        public int AffectedRows { get; set; }
    }
}
