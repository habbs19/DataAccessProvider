namespace DataAccessProvider.Abstractions;

public abstract class BaseSource
{
    protected abstract Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params);
    protected abstract Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params) where TValue : class, new();
    protected abstract Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams @params);
    protected abstract Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams @params);

}