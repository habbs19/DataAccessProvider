using DataAccessProvider.Abstractions;
using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Interfaces;

public interface IDataSource<TBaseDataSourceParams> where TBaseDataSourceParams : BaseDataSourceParams
{
    //string query, List<TDbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45
    Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue>(TBaseDataSourceParams @params) where TValue : class, new();
    Task<TBaseDataSourceParams> ExecuteReaderAsync(TBaseDataSourceParams @params);
    Task<TBaseDataSourceParams> ExecuteNonQueryAsync(TBaseDataSourceParams @params);
    Task<TBaseDataSourceParams> ExecuteScalarAsync(TBaseDataSourceParams @params);

    DbConnection GetConnection();
    DbCommand GetCommand(string query, DbConnection connection);
}
    
public interface IDataSource
{
    BaseDataSourceParams<TValue> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new();

    Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params) where TValue : class, new() where TBaseDataSourceParams : BaseDataSourceParams<TValue>;
    Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams;
    Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams;
    Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams;

    DbConnection GetConnection();
    DbCommand GetCommand(string query, DbConnection connection);
}


