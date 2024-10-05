using Oracle.ManagedDataAccess.Client;
namespace DataAccessProvider.DataSource.Params;

public class OracleSourceParams : BaseDatabaseSourceParams<OracleParameter> { }
public class OracleSourceParams<TValue> : BaseDatabaseSourceParams<OracleParameter,TValue> where TValue : class { }
