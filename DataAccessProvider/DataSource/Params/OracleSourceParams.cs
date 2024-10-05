using Oracle.ManagedDataAccess.Client;
namespace DataAccessProvider.DataSource.Params;

public class OracleSourceParams : OracleSourceParams<object> { }
public class OracleSourceParams<TValue> : BaseDatabaseSourceParams<TValue, OracleParameter> where TValue : class { }
