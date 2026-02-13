using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Oracle;

public sealed class OracleSource : BaseDatabaseSource<OracleParameter, OracleSourceParams>,
    IDataSource,
    IDataSource<OracleSourceParams>
{
    public OracleSource(string connectionString) : base(connectionString) { }

    public override DbConnection GetConnection()
    {
        return new OracleConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new OracleCommand(query, (OracleConnection)connection);
    }

    protected override DbParameter CreateDbParameter(DbCommand command, DataAccessParameter parameter)
    {
        return new OracleParameter
        {
            ParameterName = parameter.ParameterName,
            OracleDbType = DbParameterExtensions.MapDbType(parameter.DbType),
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
