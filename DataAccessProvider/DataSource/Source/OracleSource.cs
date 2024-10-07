using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;

namespace DataAccessProvider.DataSource.Source;

public sealed class OracleSource : BaseDatabaseSource<OracleParameter, OracleSourceParams>,
    IDataSource,
    IDataSource<OracleSourceParams>
{
    public OracleSource(string connectionString) : base(connectionString) { }

    public override DbConnection GetConnection()
    {
        return new OracleConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new OracleCommand(query, (OracleConnection)connection);
    }
}
