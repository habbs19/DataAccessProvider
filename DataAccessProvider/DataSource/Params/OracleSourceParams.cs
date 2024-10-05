namespace DataAccessProvider.DataSource.Params;


public class OracleSourceParams : BaseDatabaseSourceParams<object>
{
}

public class OracleSourceParams<TValue> : DatabaseSourceParams<object, TValue> where TValue : class
{

}
