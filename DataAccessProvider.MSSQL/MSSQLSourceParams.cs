using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.MSSQL;

public class MSSQLSourceParams : BaseDatabaseSourceParams { }

public class MSSQLSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class { }
