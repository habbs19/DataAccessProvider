namespace DataAccessProvider.Abstractions;

public interface IBaseFileSourceParams
{
    string FilePath { get; set; }
    string Content { get; set; }
}
public class BaseFileSourceParams : BaseDataSourceParams, IBaseFileSourceParams
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

}

public class BaseFileSourceParams<TValue> : BaseDataSourceParams<TValue>, IBaseFileSourceParams where TValue : class
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

}
