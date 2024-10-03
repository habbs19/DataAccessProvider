using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.DataSource.Source;

public sealed class MSSQLSource : BaseDatabaseSource<MSSQLSourceParams, SqlParameter>,
    IDataSource,
    IDataSource<MSSQLSourceParams>
{
    public MSSQLSource(string connectionString) : base(connectionString) { }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return connection.CreateCommand();
    }
}
