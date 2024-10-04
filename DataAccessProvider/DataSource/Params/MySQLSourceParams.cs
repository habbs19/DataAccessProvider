using MySql.Data.MySqlClient;
namespace DataAccessProvider.DataSource.Params;

public class MySQLSourceParams : DatabaseSourceParams<MySqlParameter> { }

public class MySQLSourceParams<TValue> : DatabaseSourceParams<MySqlParameter, TValue> where TValue : class { }