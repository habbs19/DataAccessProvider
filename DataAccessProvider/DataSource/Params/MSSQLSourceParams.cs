using Microsoft.Data.SqlClient;
namespace DataAccessProvider.DataSource.Params;

public class MSSQLSourceParams : MSSQLSourceParams<object> { }

public class MSSQLSourceParams<TValue> : BaseDatabaseSourceParams<TValue,SqlParameter> where TValue : class { }