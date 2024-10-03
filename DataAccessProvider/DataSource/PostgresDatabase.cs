using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.DataSource;

public sealed class PostgresDatabase : BaseDatabaseSource<PostgresSourceParams, NpgsqlParameter>,
    IDatabasePostgres,
    IDatabasePostgres<PostgresSourceParams>
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
