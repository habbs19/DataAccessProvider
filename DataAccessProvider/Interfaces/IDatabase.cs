using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Interfaces
{
    public interface IDatabase<TDataSourceType,TDbParameter> 
        where TDataSourceType : DataSourceType 
        where TDbParameter : DbParameter
    {
        Task<List<T>> ExecuteReaderAsync<T>(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45) where T : class, new();
        Task<object> ExecuteReaderAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45);
        Task<int> ExecuteNonQueryAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45);
        Task<object> ExecuteScalarAsync(string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45);

        DbConnection GetConnection();
        DbCommand GetCommand(string query, DbConnection connection);
    }

    public interface IDatabase
    {
        IDatabaseMSSQL MSSQL { get; }
    }

}
