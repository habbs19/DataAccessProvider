using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
