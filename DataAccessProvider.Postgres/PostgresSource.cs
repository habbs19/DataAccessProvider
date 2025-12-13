using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.Postgres;

public sealed class PostgresSource : BaseDatabaseSource<NpgsqlParameter,PostgresSourceParams>,
    IDataSource,
    IDataSource<PostgresSourceParams>
{
    public PostgresSource(string connectionString) : base(connectionString) { }

    protected override DbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new NpgsqlCommand(query, (NpgsqlConnection)connection);
    }
}
