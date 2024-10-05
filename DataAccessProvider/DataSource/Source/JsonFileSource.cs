using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using System.Data.Common;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DataAccessProvider.DataSource.Source;

#region Props
public partial class JsonFileSource : BaseSource
{
    private static readonly string ExceptionMessage = $"The provided parameter is not of type JsonFileSourceParams.";
    protected async override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {
        JsonFileSourceParams? jsonFileSourceParams = @params as JsonFileSourceParams;
        try
        {
            // Read file content
            string content = await File.ReadAllTextAsync(jsonFileSourceParams!.FilePath);

            // Set the result in parameters
            jsonFileSourceParams.SetValue(content);

            return jsonFileSourceParams;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading file at {jsonFileSourceParams!.FilePath}: {ex.Message}", ex);
        }
    }

    protected async override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params)
    {
        JsonFileSourceParams<TValue>? jsonFileSourceParams = @params as JsonFileSourceParams<TValue>;
        try
        {
            // Read file content
            string content = await File.ReadAllTextAsync(jsonFileSourceParams!.FilePath);
            var result = JsonSerializer.Deserialize<TValue>(content)!;

            // Set the result in parameters
            jsonFileSourceParams.SetValue(result);
            return jsonFileSourceParams;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading file at {jsonFileSourceParams!.FilePath}: {ex.Message}", ex);
        }
    }
}



#endregion Props
#region JsonFileSource
public partial class JsonFileSource : IDataSource
{
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        JsonFileSourceParams? jsonFileSourceParams = @params as JsonFileSourceParams;

        if (jsonFileSourceParams != null)
        {
            return (TBaseDataSourceParams)(object)await ExecuteNonQueryAsync(jsonFileSourceParams);
        }
        else
        {
            throw new InvalidCastException(ExceptionMessage);
        }
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        where TValue : class, new()
    {
        var sourceParams = @params as BaseDataSourceParams;
        return (TBaseDataSourceParams)(object)await ExecuteReader<TValue>(sourceParams!);
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)(object)await ExecuteReader(@params);
    }

    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}
#endregion JsonFileSource
#region JsonFileSource<>
/// <summary>
/// Represents a data source for reading and writing JSON files.
/// </summary>
public partial class JsonFileSource : IDataSource<JsonFileSourceParams>
{
    /// <summary>
    /// Writes the content provided in <paramref name="params"/> to a JSON file.
    /// </summary>
    /// <param name="params">The parameters including file path and content to write.</param>
    /// <returns>The parameters with an updated value of written bytes.</returns>
    public async Task<JsonFileSourceParams> ExecuteNonQueryAsync(JsonFileSourceParams @params)
    {
        try
        {
            // Write content to the file (overwriting any existing content)
            await File.WriteAllTextAsync(@params.FilePath, @params.Content);

            // Set the value to the number of bytes written
            @params.SetValue(@params.Content.Length);
            return @params;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error writing to file at {@params.FilePath}: {ex.Message}", ex);
        }
    }

    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(JsonFileSourceParams @params) where TValue : class, new()
    {
        return await ExecuteReader<TValue>(@params);
    }

    public async Task<JsonFileSourceParams> ExecuteReaderAsync(JsonFileSourceParams @params)
    {
        return (JsonFileSourceParams)(object)await ExecuteReader(@params);
    }

    /// <summary>
    /// Executes a scalar operation and returns a single value.
    /// </summary>
    /// <param name="params">The parameters for the operation.</param>
    /// <returns>The parameters after the scalar operation.</returns>
    public async Task<JsonFileSourceParams> ExecuteScalarAsync(JsonFileSourceParams @params)
    {
        try
        {
            // Read file content
            string content = await File.ReadAllTextAsync(@params.FilePath);

            // Assuming scalar means returning a single value (e.g., first line)
            string scalarValue = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            // Set the scalar value
            @params.SetValue(scalarValue!);

            return @params;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading scalar value from file at {@params.FilePath}: {ex.Message}", ex);
        }
    }

    
}
#endregion JsonFileSource<>
