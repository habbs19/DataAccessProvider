using DataAccessProvider.Core.Abstractions;
using Snowflake.Data.Client;
namespace DataAccessProvider.Snowflake;

public class SnowflakeSourceParams : BaseDatabaseSourceParams<SnowflakeDbParameter> { }

public class SnowflakeSourceParams<TValue> : BaseDatabaseSourceParams<SnowflakeDbParameter, TValue> where TValue : class { }