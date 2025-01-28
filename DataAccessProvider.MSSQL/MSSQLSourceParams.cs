using DataAccessProvider.Core.Abstractions;
using Microsoft.Data.SqlClient;
namespace DataAccessProvider.MSSQL;

public class MSSQLSourceParams : BaseDatabaseSourceParams<SqlParameter> { }

public class MSSQLSourceParams<TValue> : BaseDatabaseSourceParams<SqlParameter,TValue> where TValue : class { }