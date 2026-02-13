using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Snowflake;

public class SnowflakeSourceParams : BaseDatabaseSourceParams { }

public class SnowflakeSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class { }
