using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using System.Data.Common;

namespace DataAccessProvider.DataSource.Source;
public sealed class SnowflakeSource : BaseDatabaseSource<Snowflake.Data.Client.SnowflakeDbParameter, SnowflakeSourceParams>,
    IDataSource,
    IDataSource<SnowflakeSourceParams>
{
    public SnowflakeSource(string connectionString) : base(connectionString) { }

    public override DbConnection GetConnection()
    {
        return new Snowflake.Data.Client.SnowflakeDbConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new Snowflake.Data.Client.SnowflakeDbCommand((Snowflake.Data.Client.SnowflakeDbConnection)connection,query);
    }
}
