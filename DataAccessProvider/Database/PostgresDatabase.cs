using DataAccessProvider.Interfaces;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.Database;

public class PostgresDatabase : BaseDatabase<Postgres>
{
    public PostgresDatabase(string connectionString) : base(connectionString) { }

    protected override DbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    protected override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new NpgsqlCommand(query, (NpgsqlConnection)connection);
    }
}
