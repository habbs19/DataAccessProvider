using DataAccessProvider.Core.Abstractions;
using MySqlConnector;
namespace DataAccessProvider.MySql;

public class MySQLSourceParams : BaseDatabaseSourceParams<MySqlParameter> { }

public class MySQLSourceParams<TValue> : BaseDatabaseSourceParams<MySqlParameter, TValue> where TValue : class { }