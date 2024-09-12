using DataAccessProvider.Interfaces;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
