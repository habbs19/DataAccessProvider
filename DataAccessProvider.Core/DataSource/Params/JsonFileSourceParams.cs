using DataAccessProvider.Core.Abstractions;
using System.Text;
using System.Text.Json;

namespace DataAccessProvider.Core.DataSource.Params;

public class JsonFileSourceParams : BaseFileSourceParams
{
    public Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
    public JsonSerializerOptions SerializerOptions { get; set; } = null!;
}
public class JsonFileSourceParams<TValue> : BaseFileSourceParams<TValue> where TValue : class
{
    public Encoding Encoding { get; set; } = System.Text.Encoding.UTF8;
    public JsonSerializerOptions SerializerOptions { get; set; } = null!;

}