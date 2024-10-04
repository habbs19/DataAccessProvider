using Microsoft.Data.SqlClient;
namespace DataAccessProvider.DataSource.Params;

public class MSSQLSourceParams : DatabaseSourceParams<SqlParameter> { }

public class MSSQLSourceParams<TValue> : DatabaseSourceParams<SqlParameter, TValue> where TValue : class { }