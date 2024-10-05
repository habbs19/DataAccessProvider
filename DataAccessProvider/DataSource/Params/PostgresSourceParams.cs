using Npgsql;

namespace DataAccessProvider.DataSource.Params;

public class PostgresSourceParams : PostgresSourceParams<NpgsqlParameter> { }

public class PostgresSourceParams<TValue> : BaseDatabaseSourceParams<TValue, NpgsqlParameter> where TValue : class { }