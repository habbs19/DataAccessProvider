using DataAccessProvider.Abstractions;

namespace DataAccessProvider.Interfaces;

public interface IDataSourceProvider : IDataSource
{
}

public interface IDataSourceProvider<TDataSourceParams> : IDataSource<TDataSourceParams> where TDataSourceParams : BaseDataSourceParams
{
}
