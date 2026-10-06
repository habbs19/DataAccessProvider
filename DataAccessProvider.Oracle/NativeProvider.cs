using System.Data;
using System.Data.Common;
using DataAccessProvider.Core;
using DataAccessProvider.Core.Types;
using Oracle.ManagedDataAccess.Client;
namespace DataAccessProvider.Oracle;

internal sealed class NativeProvider : IRelationalProvider<OracleDatabase>
{
    private readonly string _connection;
    public NativeProvider(string connection) { ArgumentException.ThrowIfNullOrWhiteSpace(connection); using var validation = new OracleConnection(connection); _connection = connection; }
    public DatabaseCapabilities Capabilities => new() { IsolationLevels = Array.AsReadOnly(new[] { IsolationLevel.ReadCommitted, IsolationLevel.Serializable }) };
    public DbConnection CreateConnection() => new OracleConnection(_connection);
    public DbCommand CreateCommand(DbConnection connection, DatabaseCommand definition) => new OracleCommand(definition.CommandText, (OracleConnection)connection) { BindByName = true };
    public DbParameter CreateParameter(DbCommand command, DatabaseParameter definition)
    {
        if (definition.Size < 0) throw new ArgumentOutOfRangeException(nameof(definition));
        var parameter = new OracleParameter
        {
            ParameterName = definition.Name,
            OracleDbType = (OracleDbType)OracleDbTypeMapper.Instance.Map(definition.Type),
            Value = definition.Value ?? DBNull.Value,
            Direction = definition.Direction switch { DataAccessParameterDirection.Input => ParameterDirection.Input, DataAccessParameterDirection.Output => ParameterDirection.Output, DataAccessParameterDirection.InputOutput => ParameterDirection.InputOutput, DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue, _ => throw new ArgumentOutOfRangeException(nameof(definition)) }
        };
        if (definition.Size.HasValue) parameter.Size = definition.Size.Value;
        if (definition.Precision.HasValue) parameter.Precision = definition.Precision.Value;
        if (definition.Scale.HasValue) parameter.Scale = definition.Scale.Value;
        return parameter;
    }
    public void Validate(DatabaseCommand command) { if (command.Parameters.Any(p => p.Direction != DataAccessParameterDirection.Input && p.Type is DataAccessDbType.String or DataAccessDbType.NVarChar or DataAccessDbType.VarChar or DataAccessDbType.Binary && (!p.Size.HasValue || p.Size <= 0))) throw new ArgumentException("Variable-width Oracle output parameters require capacity."); }
    public bool IsTransient(Exception exception) => exception is OracleException oracle && oracle.Number is 54 or 60;
    public HealthFailure ClassifyHealthFailure(Exception exception) => exception is OracleException oracle && oracle.Number is 1017 or 28000 ? HealthFailure.Authentication : exception is ArgumentException ? HealthFailure.Configuration : exception is DbException ? HealthFailure.Connectivity : HealthFailure.Unknown;
    public object? ReadOutput(DbParameter parameter) => parameter.Value is global::Oracle.ManagedDataAccess.Types.OracleDecimal number ? (number.IsNull ? null : number.Value) : parameter.Value is DBNull ? null : parameter.Value;
}
