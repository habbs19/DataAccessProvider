using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public interface IStaticCodeParams
{
  object Content { get; set; }
}
public class StaticCodeParams : BaseDataSourceParams, IStaticCodeParams
{
    public object Content { get; set; } = null!;
}

public class StaticCodeParams<T> : BaseDataSourceParams<T>, IStaticCodeParams where T : class
{
    public object Content { get; set; } = null!;
}
