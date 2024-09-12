using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Interfaces
{
    public interface IDatabase<TDatabaseType> where TDatabaseType : DatabaseType
    {
        Task<List<T>> ExecuteQueryAsync<T>(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure) where T : class, new();
        Task<object> ExecuteReaderAsync(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure);
        Task<int> ExecuteNonQueryAsync(string query, List<DbParameter>? parameters = null, int timeout = 45, CommandType commandType = CommandType.StoredProcedure);
    }

}
