using DataAccessProvider.Interfaces;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.Database;

public sealed class PostgresDatabase : BaseDatabase<Postgres,NpgsqlParameter>,IDatabasePostgres
{
    public PostgresDatabase(string connectionString) : base(connectionString) { }

    public override DbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new NpgsqlCommand(query, (NpgsqlConnection)connection);
    }
}
