using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Postgres;

public sealed class PostgresSource : BaseDatabaseSource<PostgresSourceParams>,
    IDataSource,
    IDataSource<PostgresSourceParams>
{
    public PostgresSource(string connectionString, IResiliencePolicy? resiliencePolicy = null)
        : base(connectionString, resiliencePolicy) { }

    public override DbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new NpgsqlCommand(query, (NpgsqlConnection)connection);
    }

    protected override DbParameter CreateDbParameter(DbCommand command, DataAccessParameter parameter)
    {
        return new NpgsqlParameter
        {
            ParameterName = parameter.ParameterName,
            NpgsqlDbType = DbParameterExtensions.MapDbType(parameter.DbType),
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
