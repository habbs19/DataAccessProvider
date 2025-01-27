using DataAccessProvider.Core.Abstractions;
using Npgsql;

namespace DataAccessProvider.Postgres;

public class PostgresSourceParams : BaseDatabaseSourceParams<NpgsqlParameter> { }

public class PostgresSourceParams<TValue> : BaseDatabaseSourceParams<NpgsqlParameter,TValue> where TValue : class { }