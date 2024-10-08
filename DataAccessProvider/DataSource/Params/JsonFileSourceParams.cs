using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public class JsonFileSourceParams : BaseFileSourceParams { }
public class JsonFileSourceParams<TValue> : BaseFileSourceParams<TValue> where TValue : class { }