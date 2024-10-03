using DataAccessProvider.Abstractions;
using DataAccessProvider.Types;
using System.Data.Common;

namespace DataAccessProvider.Interfaces;

public interface IDataSourceFactory
{
    IDataSource<IBaseDataSourceParams> CreateDataSource<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams;
    IDataSource CreateDataSource(DataSourceTypeEnum sourceType);

    IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams;
}
