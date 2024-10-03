using DataAccessProvider.Abstractions;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataAccessProvider.DataSource.Params;

public class MSSQLSourceParams : DatabaseParams<SqlParameter>
{
}

public class MSSQLSourceParams<TValue> : DatabaseParams<SqlParameter,TValue> where TValue : class
{

}
