using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.DataSource;

public sealed class MSSQLDatabase : BaseDatabaseSource<MSSQLSourceParams, SqlParameter>, 
    IDatabaseMSSQL,
    IDatabaseMSSQL<MSSQLSourceParams>
{
    public MSSQLDatabase(string connectionString) : base(connectionString)
    {
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        throw new NotImplementedException();
    }
}
