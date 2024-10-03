using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessProvider.Abstractions;

namespace DataAccessProvider.Interfaces;

public interface IDatabaseSource
{
    //string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45
    Task<List<T>> ExecuteReaderAsync<T>(BaseDataSourceParams @params) where T : class, new();
    Task<object> ExecuteReaderAsync(BaseDataSourceParams @params);
    Task<int> ExecuteNonQueryAsync(BaseDataSourceParams @params);
    Task<object> ExecuteScalarAsync(BaseDataSourceParams @params);

    DbConnection GetConnection();
    DbCommand GetCommand(string query, DbConnection connection);
}
