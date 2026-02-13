using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.MSSQL;

public sealed class MSSQLSource : BaseDatabaseSource<MSSQLSourceParams>,
    IDataSource,
    IDataSource<MSSQLSourceParams>
{
    public MSSQLSource(string connectionString, IResiliencePolicy? resiliencePolicy = null)
        : base(connectionString, resiliencePolicy)
    {
    }

    public override DbConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new SqlCommand(query, (SqlConnection)connection);
    }

    protected override DbParameter CreateDbParameter(DbCommand command, DataAccessParameter parameter)
    {
        return new SqlParameter
        {
            ParameterName = parameter.ParameterName,
            SqlDbType = DbParameterExtensions.MapDbType(parameter.DbType),
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
