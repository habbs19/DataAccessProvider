using MySql.Data.MySqlClient;
namespace DataAccessProvider.DataSource.Params;

public class MySQLSourceParams : BaseDatabaseSourceParams<MySqlParameter> { }

public class MySQLSourceParams<TValue> : BaseDatabaseSourceParams<MySqlParameter,TValue> where TValue : class { }