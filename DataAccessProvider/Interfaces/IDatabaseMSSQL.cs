using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;

namespace DataAccessProvider.Interfaces;

public interface IDatabaseMSSQL<IMSSQLDataSourceParams> : IDataSource<IMSSQLDataSourceParams> where IMSSQLDataSourceParams : BaseDataSourceParams { }

public interface IDatabaseMSSQL : IDataSource { }