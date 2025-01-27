using DataAccessProvider.Core.Abstractions;
using Oracle.ManagedDataAccess.Client;
namespace DataAccessProvider.Oracle;

public class OracleSourceParams : BaseDatabaseSourceParams<OracleParameter> { }
public class OracleSourceParams<TValue> : BaseDatabaseSourceParams<OracleParameter,TValue> where TValue : class { }
