namespace DataAccessProvider.DataSource.Params;


public class OracleSourceParams : DatabaseSourceParams<object>
{
}

public class OracleSourceParams<TValue> : DatabaseSourceParams<object, TValue> where TValue : class
{

}
