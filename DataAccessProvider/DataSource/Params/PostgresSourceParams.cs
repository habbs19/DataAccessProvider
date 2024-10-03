using Npgsql;

namespace DataAccessProvider.DataSource.Params;

public class PostgresSourceParams : DatabaseSourceParams<NpgsqlParameter>
{
}

public class PostgresSourceParams<TValue> : DatabaseParams<NpgsqlParameter, TValue> where TValue : class
{

}
