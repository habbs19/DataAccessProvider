using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
using Snowflake.Data.Client;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Snowflake;
public sealed class SnowflakeSource : BaseDatabaseSource<SnowflakeSourceParams>,
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
        return new SnowflakeDbCommand((SnowflakeDbConnection)connection, query);
    }

    protected override DbParameter CreateDbParameter(DbCommand command, DataAccessParameter parameter)
    {
        return new SnowflakeDbParameter
        {
            ParameterName = parameter.ParameterName,
            SFDataType = DbParameterExtensions.MapDbType(parameter.DbType),
            Value = parameter.Value ?? DBNull.Value,
            Direction = MapDirection(parameter.Direction),
            Size = parameter.Size
        };
    }

    private static ParameterDirection MapDirection(DataAccessParameterDirection direction) => direction switch
    {
        DataAccessParameterDirection.Input => ParameterDirection.Input,
        DataAccessParameterDirection.Output => ParameterDirection.Output,
        DataAccessParameterDirection.InputOutput => ParameterDirection.InputOutput,
        DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported parameter direction.")
    };
}
