using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.Database
{
    public sealed class MSSQLDatabase : BaseDatabase<MSSQL>
    {
        public MSSQLDatabase(string connectionString): base(connectionString) { }

        protected override DbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        protected override DbCommand GetCommand(string query, DbConnection connection)
        {
            return new SqlCommand(query, (SqlConnection)connection);
        }
    }

}
