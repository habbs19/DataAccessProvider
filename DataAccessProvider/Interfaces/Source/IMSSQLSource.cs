using DataAccessProvider.Abstractions;
namespace DataAccessProvider.Interfaces.Source;

public interface IMSSQLSource<IMSSQLDataSourceParams> : IDataSource<IMSSQLDataSourceParams> where IMSSQLDataSourceParams : BaseDataSourceParams { }

//public interface IDatabaseMSSQL : IDataSource { }