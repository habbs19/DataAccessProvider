using DataAccessProvider.Core.Abstractions;
using MySql.Data.MySqlClient;
namespace DataAccessProvider.MySql;

public class MySQLSourceParams : BaseDatabaseSourceParams<MySqlParameter> { }

public class MySQLSourceParams<TValue> : BaseDatabaseSourceParams<MySqlParameter, TValue> where TValue : class { }