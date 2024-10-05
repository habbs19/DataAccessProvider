using Npgsql;

namespace DataAccessProvider.DataSource.Params;

public class PostgresSourceParams : BaseDatabaseSourceParams<NpgsqlParameter>
{
}

public class PostgresSourceParams<TValue> : DatabaseSourceParams<NpgsqlParameter, TValue> where TValue : class
{

}
