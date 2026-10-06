using System.Data.Common;
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core;

/// <summary>Compatibility bridge. New custom providers can implement IRelationalProvider directly.</summary>
public sealed class SourceProvider<TProvider>(BaseDatabaseSource source, DatabaseCapabilities? capabilities = null, Action<DbCommand>? configureCommand = null) : IRelationalProvider<TProvider>
{
    public DatabaseCapabilities Capabilities { get; } = capabilities ?? new();
    public DbConnection CreateConnection() => source.GetConnection();
    public DbCommand CreateCommand(DbConnection connection, DatabaseCommand definition)
    {
        var command = source.GetCommand(definition.CommandText, connection);
        try { configureCommand?.Invoke(command); return command; }
        catch { command.Dispose(); throw; }
    }
    public DbParameter CreateParameter(DbCommand command, DatabaseParameter definition) => source.BuildParameter(command, new DataAccessParameter
    {
        ParameterName = definition.Name,
        DbType = definition.Type,
        Value = definition.Value,
        Direction = definition.Direction,
        Size = definition.Size ?? -1,
        Precision = definition.Precision,
        Scale = definition.Scale
    });
    public object? ReadOutput(DbParameter parameter) => source.ReadOutputValue(parameter);
}
