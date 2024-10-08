using DataAccessProvider.Abstractions;
namespace DataAccessProvider.DataSource.Params;
public class XmlFileSourceParams : BaseFileSourceParams { }
public class XmlFileSourceParams<TValue> : BaseFileSourceParams<TValue> where TValue : class { }