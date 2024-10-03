using DataAccessProvider.DataSource.Params;
using System.Data.Common;

namespace DataAccessProvider.Interfaces;

public interface IJsonFileSource<IJsonFileSourceParams> : IDataSource<IJsonFileSourceParams> where IJsonFileSourceParams : JsonFileSourceParams { }

public interface IJsonFileSource : IDataSource { }