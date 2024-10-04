using DataAccessProvider.Abstractions;
namespace DataAccessProvider.Interfaces;

public interface IDataSource
{
    Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new();

    Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue,TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams<TValue> where TValue : class, new();
    Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams;
    Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams;
    Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams;
}

public interface IDataSource<TBaseDataSourceParams> where TBaseDataSourceParams : BaseDataSourceParams
{
    Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue>(TBaseDataSourceParams @params) where TValue : class, new();
    Task<TBaseDataSourceParams> ExecuteReaderAsync(TBaseDataSourceParams @params);
    Task<TBaseDataSourceParams> ExecuteNonQueryAsync(TBaseDataSourceParams @params);
    Task<TBaseDataSourceParams> ExecuteScalarAsync(TBaseDataSourceParams @params);
}
    



