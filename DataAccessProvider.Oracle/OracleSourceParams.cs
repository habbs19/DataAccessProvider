using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Oracle;

public class OracleSourceParams : BaseDatabaseSourceParams { }

public class OracleSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class { }
