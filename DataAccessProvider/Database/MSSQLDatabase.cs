using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace DataAccessProvider.Database
{
    public class MSSQLDatabase : BaseDatabase<MSSQL>
    {
        public MSSQLDatabase(string connectionString): base(connectionString) { }

        protected override DbCommand GetCommand(string query, DbConnection connection)
        {
            return new SqlCommand(query, (SqlConnection)connection);
        }

        protected override DbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }

}
