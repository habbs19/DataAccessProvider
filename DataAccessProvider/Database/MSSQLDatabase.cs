using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.Database;

public sealed class MSSQLDatabase : BaseDatabase<MSSQL,SqlParameter>, IDatabaseMSSQL
{
    public MSSQLDatabase(string connectionString): base(connectionString) { }

    public override DbConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new SqlCommand(query, (SqlConnection)connection);
    }
}
