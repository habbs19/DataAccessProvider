using DataAccessProvider.DataSource.Params;
namespace DataAccessProvider.Interfaces.Source;

public interface IStaticCodeSource<IJsonFileSourceParams> : IDataSource<IJsonFileSourceParams> where IJsonFileSourceParams : JsonFileSourceParams { }
