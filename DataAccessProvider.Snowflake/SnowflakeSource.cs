using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using System.Data.Common;
using Snowflake.Data.Client;

namespace DataAccessProvider.Snowflake;
public sealed class SnowflakeSource : BaseDatabaseSource<SnowflakeDbParameter, SnowflakeSourceParams>,
    IDataSource,
    IDataSource<SnowflakeSourceParams>
{
    public SnowflakeSource(string connectionString) : base(connectionString) { }

    public override DbConnection GetConnection()
    {
        return new SnowflakeDbConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new SnowflakeDbCommand((SnowflakeDbConnection)connection,query);
    }
}
