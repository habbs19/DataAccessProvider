using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Postgres;

public class PostgresSourceParams : BaseDatabaseSourceParams { }

public class PostgresSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class { }
