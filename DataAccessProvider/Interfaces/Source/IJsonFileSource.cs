using DataAccessProvider.DataSource.Params;
namespace DataAccessProvider.Interfaces.Source;

//public interface IJsonFileSource : IDataSource { }
public interface IJsonFileSource<IJsonFileSourceParams> : IDataSource<IJsonFileSourceParams> where IJsonFileSourceParams : JsonFileSourceParams { }
