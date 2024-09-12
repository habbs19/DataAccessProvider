using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessProvider.Interfaces
{
    public interface IDatabase<TDatabaseType> where TDatabaseType : DatabaseType
    {
        Task<object> ExecuteReaderAsync(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure);
        Task<int> ExecuteNonQueryAsync<TConnection, TCommand>(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure);
    }

}
