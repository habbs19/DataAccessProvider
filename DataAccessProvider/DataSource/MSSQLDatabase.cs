using DataAccessProvider.Interfaces;
using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.DataSource;

public sealed class MSSQLDatabase : BaseDatabase<MSSQL,SqlParameter>, IDatabaseMSSQL
{
    public MSSQLDatabase(string connectionString): base(connectionString) { }

    /// <summary>
    /// Creates and returns a new <see cref="SqlConnection"/> object using the provided connection string.
    /// </summary>
    /// <returns>
    /// A <see cref="SqlConnection"/> object initialized with the current connection string.
    /// </returns>
    /// <remarks>
    /// The returned <see cref="SqlConnection"/> object must be properly opened and disposed by the caller.
    /// </remarks>
    public override DbConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    /// <summary>
    /// Creates and returns a new <see cref="SqlCommand"/> object using the provided query and connection.
    /// </summary>
    /// <param name="query">The SQL query to be executed by the <see cref="SqlCommand"/>.</param>
    /// <param name="connection">The <see cref="SqlConnection"/> object that the command will be associated with.</param>
    /// <returns>
    /// A <see cref="SqlCommand"/> object initialized with the specified query and connection.
    /// </returns>
    /// <remarks>
    /// The <paramref name="connection"/> must be an instance of <see cref="SqlConnection"/> and must be opened before executing the command.
    /// </remarks>
    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new SqlCommand(query, (SqlConnection)connection);
    }

}
