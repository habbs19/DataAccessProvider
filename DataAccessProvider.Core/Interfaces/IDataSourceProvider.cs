using DataAccessProvider.Core.Abstractions;
namespace DataAccessProvider.Core.Interfaces;

public interface IDataSourceProvider : IDataSource { }
public interface IDataSourceProvider<TDataSourceParams> : IDataSource<TDataSourceParams> 
    where TDataSourceParams : BaseDataSourceParams { }