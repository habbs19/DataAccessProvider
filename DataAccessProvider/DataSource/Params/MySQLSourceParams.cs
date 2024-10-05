using MySql.Data.MySqlClient;
namespace DataAccessProvider.DataSource.Params;

public class MySQLSourceParams : MySQLSourceParams<object> { }

public class MySQLSourceParams<TValue> : BaseDatabaseSourceParams<TValue,MySqlParameter> where TValue : class { }