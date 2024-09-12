using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Interfaces
{
    public interface IDatabase<TDatabaseType> where TDatabaseType : DatabaseType
    {
        Task<object> ExecuteReaderAsync(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure);
        Task<int> ExecuteNonQueryAsync<TConnection, TCommand>(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure);
    }

}
