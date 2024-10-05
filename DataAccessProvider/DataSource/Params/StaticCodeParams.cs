using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public interface IStaticCodeParams
{
  object Content { get; set; }
}
public class StaticCodeParams : StaticCodeParams<object>, IStaticCodeParams { }

public class StaticCodeParams<TValue> : BaseDataSourceParams<TValue>, IStaticCodeParams where TValue : class
{
    public object Content { get; set; } = null!;
}
