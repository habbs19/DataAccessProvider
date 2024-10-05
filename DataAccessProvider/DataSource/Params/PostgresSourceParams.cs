using Npgsql;

namespace DataAccessProvider.DataSource.Params;

public class PostgresSourceParams : BaseDatabaseSourceParams<NpgsqlParameter> { }

public class PostgresSourceParams<TValue> : BaseDatabaseSourceParams<NpgsqlParameter,TValue> where TValue : class { }