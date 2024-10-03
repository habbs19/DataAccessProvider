using Microsoft.Data.SqlClient;
namespace DataAccessProvider.DataSource.Params;

public class MSSQLSourceParams : DatabaseSourceParams<SqlParameter> { }

public class MSSQLSourceParams<TValue> : DatabaseParams<SqlParameter, TValue> where TValue : class { }