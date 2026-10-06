using DataAccessProvider.Core;
using Oracle.ManagedDataAccess.Client;
namespace DataAccessProvider.Oracle;

public sealed class OracleDatabase;
public sealed class OracleClient : DatabaseClient<OracleDatabase>
{
    public OracleClient(string connectionString, DatabaseClientOptions? options = null)
        : base(new NativeProvider(connectionString), options, ResourceOwnership.Owned) { }
}
public static class OracleAdvanced
{
    public static Task<T> ExecuteAsync<T>(IDatabaseSession session,
        Func<OracleConnection, OracleTransaction?, CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        => session.ExecuteNativeAsync((connection, transaction, ct) => action((OracleConnection)connection, (OracleTransaction?)transaction, ct), cancellationToken);
}
