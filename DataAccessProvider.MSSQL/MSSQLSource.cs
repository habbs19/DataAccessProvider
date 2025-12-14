using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace DataAccessProvider.MSSQL;

public sealed class MSSQLSource : BaseDatabaseSource<SqlParameter, MSSQLSourceParams>,
    IDataSource,
    IDataSource<MSSQLSourceParams>
{
    public MSSQLSource(string connectionString) : base(connectionString) { }

    public override DbConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new SqlCommand(query, (SqlConnection)connection);
    }
}
