using DataAccessProvider.Abstractions;
using Microsoft.Data.SqlClient;
namespace DataAccessProvider.DataSource.Params;

public class MSSQLSourceParams : BaseDatabaseSourceParams<SqlParameter> { }

public class MSSQLSourceParams<TValue> : BaseDatabaseSourceParams<SqlParameter,TValue> where TValue : class { }