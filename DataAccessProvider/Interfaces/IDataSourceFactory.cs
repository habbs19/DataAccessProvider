using DataAccessProvider.Abstractions;
using DataAccessProvider.Types;
using System.Data.Common;

namespace DataAccessProvider.Interfaces;

public interface IDataSourceFactory
{
    IDataSource CreateDataSource(BaseDataSourceParams baseDataSourceParams);
    IDataSource CreateDataSource<TValue>(BaseDataSourceParams<TValue> baseDataSourceParams) where TValue : class;

    IDataSource<IBaseDataSourceParams> CreateDataSource<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams;

    IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams;
}
