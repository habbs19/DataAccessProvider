using DataAccessProvider.Core;
using Snowflake.Data.Client;
namespace DataAccessProvider.Snowflake;

public sealed class SnowflakeDatabase;
public sealed class SnowflakeClient : DatabaseClient<SnowflakeDatabase>
{
    public SnowflakeClient(string connectionString, DatabaseClientOptions? options = null)
        : base(new NativeProvider(connectionString), options, ResourceOwnership.Owned) { }
}
public static class SnowflakeAdvanced
{
    public static Task<T> ExecuteAsync<T>(IDatabaseSession session,
        Func<SnowflakeDbConnection, SnowflakeDbTransaction?, CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        => session.ExecuteNativeAsync((connection, transaction, ct) => action((SnowflakeDbConnection)connection, (SnowflakeDbTransaction?)transaction, ct), cancellationToken);
}
