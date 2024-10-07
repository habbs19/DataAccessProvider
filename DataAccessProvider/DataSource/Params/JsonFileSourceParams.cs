using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public interface IJsonFileSourceParams
{
    string FilePath { get; set; }
    string Content { get; set; }
}
public class JsonFileSourceParams : BaseDataSourceParams, IJsonFileSourceParams
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

}

public class JsonFileSourceParams<TValue> : BaseDataSourceParams<TValue>, IJsonFileSourceParams where TValue : class
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

}
