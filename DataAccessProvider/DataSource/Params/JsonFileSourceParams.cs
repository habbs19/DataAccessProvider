using DataAccessProvider.Abstractions;

namespace DataAccessProvider.DataSource.Params;

public interface IJsonFileSourceParams
{
    string FilePath { get; set; }
    string Content { get; set; }
}
public class JsonFileSourceParams : JsonFileSourceParams<object>, IJsonFileSourceParams { }

public class JsonFileSourceParams<T> : BaseDataSourceParams<T>, IJsonFileSourceParams where T : class
{
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = @"{Content:'Default file content'}";
}
