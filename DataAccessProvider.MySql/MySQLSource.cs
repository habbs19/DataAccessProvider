using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using MySqlConnector;
using System.Data.Common;

namespace DataAccessProvider.MySql;

public sealed class MySQLSource : BaseDatabaseSource<MySqlParameter,MySQLSourceParams>,
    IDataSource,
    IDataSource<MySQLSourceParams>
{
    public MySQLSource(string connectionString, IResiliencePolicy? resiliencePolicy = null) : base(connectionString, resiliencePolicy) { }

    public override DbConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new MySqlCommand(query, (MySqlConnection)connection);
    }
}
