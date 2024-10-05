using Microsoft.Data.SqlClient;
namespace DataAccessProvider.DataSource.Params;

public class MSSQLSourceParams : BaseDatabaseSourceParams<SqlParameter> { }

public class MSSQLSourceParams<TValue> : DatabaseSourceParams<SqlParameter, TValue> where TValue : class { }