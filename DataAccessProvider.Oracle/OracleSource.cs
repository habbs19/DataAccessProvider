using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;

namespace DataAccessProvider.Oracle;

public sealed class OracleSource : BaseDatabaseSource<OracleSourceParams>,
    IDataSource,
    IDataSource<OracleSourceParams>, IDatabaseTransactionProvider<OracleSourceParams>
{
    public OracleSource(string connectionString) : base(connectionString) { }
    public OracleSource(string connectionString, IResiliencePolicy? policy) : base(connectionString, policy) { }

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
        var result = new OracleParameter
        {
            ParameterName = parameter.ParameterName,
            OracleDbType = (OracleDbType)OracleSourceParams.DbTypeMapper.Map(parameter.DbType),
            Value = parameter.Value ?? DBNull.Value,
            Direction = MapDirection(parameter.Direction)
        }; if (parameter.Size >= 0) result.Size = parameter.Size; return result;
    }

    protected override object? ReadParameterValue(DbParameter parameter) => parameter.Value is global::Oracle.ManagedDataAccess.Types.OracleDecimal value ? (value.IsNull ? null : value.Value) : base.ReadParameterValue(parameter); private static ParameterDirection MapDirection(DataAccessParameterDirection direction) => direction switch
    {
        DataAccessParameterDirection.Input => ParameterDirection.Input,
        DataAccessParameterDirection.Output => ParameterDirection.Output,
        DataAccessParameterDirection.InputOutput => ParameterDirection.InputOutput,
        DataAccessParameterDirection.ReturnValue => ParameterDirection.ReturnValue,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unsupported parameter direction.")
    };
    public Task ExecuteInTransactionAsync(
            Func<IDatabaseTransaction, CancellationToken, Task> operation,
            IsolationLevel? isolationLevel = null,
            CancellationToken cancellationToken = default) =>
            ExecuteInTransactionCoreAsync(operation, isolationLevel, cancellationToken);

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<IDatabaseTransaction, CancellationToken, Task<TResult>> operation,
        IsolationLevel? isolationLevel = null,
        CancellationToken cancellationToken = default) =>
        ExecuteInTransactionCoreAsync(operation, isolationLevel, cancellationToken);
}
