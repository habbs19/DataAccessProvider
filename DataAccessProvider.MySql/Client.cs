using DataAccessProvider.Core;
using MySqlConnector;
namespace DataAccessProvider.MySql;

public sealed class MySql;
public sealed class MySqlClient : DatabaseClient<MySql>
{
    public MySqlClient(string connectionString, DatabaseClientOptions? options = null)
        : base(new NativeProvider(connectionString), options, ResourceOwnership.Owned) { }
}
public static class MySqlAdvanced
{
    public static Task<T> ExecuteAsync<T>(IDatabaseSession session,
        Func<MySqlConnection, MySqlTransaction?, CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        => session.ExecuteNativeAsync((connection, transaction, ct) => action((MySqlConnection)connection, (MySqlTransaction?)transaction, ct), cancellationToken);
}
