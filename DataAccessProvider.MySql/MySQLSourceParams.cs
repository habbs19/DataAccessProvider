using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.MySql;

public class MySQLSourceParams : BaseDatabaseSourceParams { }

public class MySQLSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class { }
