namespace DataAccessProvider.DataSource.Params;


public class OracleSourceParams : DatabaseSourceParams<object>
{
}

public class OracleSourceParams<TValue> : DatabaseParams<object, TValue> where TValue : class
{

}
