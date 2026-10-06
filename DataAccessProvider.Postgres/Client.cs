using DataAccessProvider.Core;
using Npgsql;
namespace DataAccessProvider.Postgres;

public sealed class PostgreSql;
public sealed class PostgreSqlClient : DatabaseClient<PostgreSql>
{
    public PostgreSqlClient(string connectionString, DatabaseClientOptions? options = null)
        : base(new NativeProvider(connectionString), options, ResourceOwnership.Owned) { }
}
public static class PostgreSqlAdvanced
{
    public static Task<T> ExecuteAsync<T>(IDatabaseSession session,
        Func<NpgsqlConnection, NpgsqlTransaction?, CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        => session.ExecuteNativeAsync((connection, transaction, ct) => action((NpgsqlConnection)connection, (NpgsqlTransaction?)transaction, ct), cancellationToken);
}
