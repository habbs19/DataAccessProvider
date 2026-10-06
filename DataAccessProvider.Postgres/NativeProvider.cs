using System.Data;
using System.Data.Common;
using DataAccessProvider.Core;
using DataAccessProvider.Core.Types;
using Npgsql;
namespace DataAccessProvider.Postgres;

internal sealed class NativeProvider : IRelationalProvider<PostgreSql>
{
    private readonly string _connection;
    public NativeProvider(string connection) { ArgumentException.ThrowIfNullOrWhiteSpace(connection); using var validation = new NpgsqlConnection(connection); _connection = connection; }
    public DatabaseCapabilities Capabilities => new();
    public DbConnection CreateConnection() => new NpgsqlConnection(_connection);
    public DbCommand CreateCommand(DbConnection connection, DatabaseCommand definition) => new NpgsqlCommand(definition.CommandText, (NpgsqlConnection)connection);
    public DbParameter CreateParameter(DbCommand command, DatabaseParameter definition)
    {
        if (definition.Size < 0) throw new ArgumentOutOfRangeException(nameof(definition));
        var parameter = new NpgsqlParameter
        {
            ParameterName = definition.Name,
            NpgsqlDbType = (NpgsqlTypes.NpgsqlDbType)PostgresDbTypeMapper.Instance.Map(definition.Type),
            Value = definition.Value ?? DBNull.Value,
            Direction = definition.Direction switch { DataAccessParameterDirection.Input => ParameterDirection.Input, DataAccessParameterDirection.Output => ParameterDirection.Output, DataAccessParameterDirection.InputOutput => ParameterDirection.InputOutput, DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue, _ => throw new ArgumentOutOfRangeException(nameof(definition)) }
        };
        if (definition.Size.HasValue) parameter.Size = definition.Size.Value;
        if (definition.Precision.HasValue) parameter.Precision = definition.Precision.Value;
        if (definition.Scale.HasValue) parameter.Scale = definition.Scale.Value;
        return parameter;
    }
    public void Validate(DatabaseCommand command) { }
    public bool IsTransient(Exception exception) => exception is DbException { IsTransient: true };
    public HealthFailure ClassifyHealthFailure(Exception exception) => exception is PostgresException pg && pg.SqlState is "28000" or "28P01" ? HealthFailure.Authentication : exception is ArgumentException ? HealthFailure.Configuration : exception is DbException ? HealthFailure.Connectivity : HealthFailure.Unknown;

}
