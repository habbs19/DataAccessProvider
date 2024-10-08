using Snowflake.Data.Client;
namespace DataAccessProvider.DataSource.Params;

public class SnowflakeSourceParams : BaseDatabaseSourceParams<SnowflakeDbParameter> { }

public class SnowflakeSourceParams<TValue> : BaseDatabaseSourceParams<SnowflakeDbParameter, TValue> where TValue : class { }