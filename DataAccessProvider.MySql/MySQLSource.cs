using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
using MySqlConnector;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.MySql;

public sealed class MySQLSource : BaseDatabaseSource<MySqlParameter, MySQLSourceParams>,
    IDataSource,
    IDataSource<MySQLSourceParams>
{
    public MySQLSource(string connectionString, IResiliencePolicy? resiliencePolicy = null) : base(connectionString, resiliencePolicy) { }

    public override DbConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new MySqlCommand(query, (MySqlConnection)connection);
    }

    protected override DbParameter CreateDbParameter(DbCommand command, DataAccessParameter parameter)
    {
        return new MySqlParameter
        {
            ParameterName = parameter.ParameterName,
            MySqlDbType = DbParameterExtensions.MapDbType(parameter.DbType),
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
