using DataAccessProvider.Core;
using Microsoft.Data.SqlClient;
namespace DataAccessProvider.MSSQL;

public sealed class SqlServer;
public sealed class SqlServerClient : DatabaseClient<SqlServer>
{
    public SqlServerClient(string connectionString, DatabaseClientOptions? options = null)
        : base(new NativeProvider(connectionString), options, ResourceOwnership.Owned) { }
}
public static class SqlServerAdvanced
{
    public static Task<T> ExecuteAsync<T>(IDatabaseSession session,
        Func<SqlConnection, SqlTransaction?, CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
        => session.ExecuteNativeAsync((connection, transaction, ct) => action((SqlConnection)connection, (SqlTransaction?)transaction, ct), cancellationToken);
}
