using System.Data;
using System.Data.Common;
using DataAccessProvider.Core;
using DataAccessProvider.Core.Types;
using Microsoft.Data.SqlClient;
namespace DataAccessProvider.MSSQL;

internal sealed class NativeProvider : IRelationalProvider<SqlServer>
{
    private readonly string _connection;
    public NativeProvider(string connection) { ArgumentException.ThrowIfNullOrWhiteSpace(connection); using var validation = new SqlConnection(connection); _connection = connection; }
    public DatabaseCapabilities Capabilities => new();
    public DbConnection CreateConnection() => new SqlConnection(_connection);
    public DbCommand CreateCommand(DbConnection connection, DatabaseCommand definition) => new SqlCommand(definition.CommandText, (SqlConnection)connection);
    public DbParameter CreateParameter(DbCommand command, DatabaseParameter definition)
    {
        if (definition.Size < -1) throw new ArgumentOutOfRangeException(nameof(definition));
        var parameter = new SqlParameter
        {
            ParameterName = definition.Name,
            SqlDbType = (SqlDbType)SqlServerDbTypeMapper.Instance.Map(definition.Type),
            Value = definition.Value ?? DBNull.Value,
            Direction = definition.Direction switch { DataAccessParameterDirection.Input => ParameterDirection.Input, DataAccessParameterDirection.Output => ParameterDirection.Output, DataAccessParameterDirection.InputOutput => ParameterDirection.InputOutput, DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue, _ => throw new ArgumentOutOfRangeException(nameof(definition)) }
        };
        if (definition.Size.HasValue) parameter.Size = definition.Size.Value;
        if (definition.Precision.HasValue) parameter.Precision = definition.Precision.Value;
        if (definition.Scale.HasValue) parameter.Scale = definition.Scale.Value;
        return parameter;
    }
    public void Validate(DatabaseCommand command) { }
    public bool IsTransient(Exception exception) => exception is SqlException sql && sql.Number is 1205 or 40613 or 40197 or 40501;
    public HealthFailure ClassifyHealthFailure(Exception exception) => exception is SqlException sql && sql.Number == 18456 ? HealthFailure.Authentication : exception is ArgumentException ? HealthFailure.Configuration : exception is DbException ? HealthFailure.Connectivity : HealthFailure.Unknown;

}
