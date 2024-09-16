using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Interfaces
{
    public interface IDatabase<TDatabaseType,TDbParameter> where TDatabaseType : DatabaseType where TDbParameter : DbParameter
    {
        Task<List<T>> ExecuteQueryAsync<T>(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45) where T : class, new();
        Task<object> ExecuteReaderAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45);
        Task<int> ExecuteNonQueryAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45);
        Task<object> ExecuteScalarAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45);

        DbConnection GetConnection();
        DbCommand GetCommand(string query, DbConnection connection);
    }

}
