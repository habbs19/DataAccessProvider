using Npgsql;

namespace DataAccessProvider.DataSource.Params;

public class PostgresSourceParams : DatabaseParams<NpgsqlParameter>
{
}

public class PostgresSourceParams<TValue> : DatabaseParams<NpgsqlParameter, TValue> where TValue : class
{

}
