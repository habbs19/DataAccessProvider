using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using System.Net.Http;
using System.Text.Json;

namespace DataAccessProvider.DataSource.Source;

#region Props
public partial class JsonFileSource : BaseSource
{
    private static readonly string ExceptionMessage = $"The provided parameter is not of type JsonFileSourceParams.";
    private void CheckFileExists(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found at {filePath}");
        }
    }
    
    protected async override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams @params)
    {
        JsonFileSourceParams? jsonFileSourceParams = @params as JsonFileSourceParams;
        CheckFileExists(jsonFileSourceParams!.FilePath);
        try
        {
            // Write content to the file (overwriting any existing content)
            await File.WriteAllTextAsync(jsonFileSourceParams!.FilePath, jsonFileSourceParams.Content);

            // Set the value to the number of bytes written
            jsonFileSourceParams.SetValue(jsonFileSourceParams.Content.Length);
            return jsonFileSourceParams;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error writing to file at {jsonFileSourceParams!.FilePath}: {ex.Message}", ex);
        }
    }

    protected async override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {
        JsonFileSourceParams? jsonFileSourceParams = @params as JsonFileSourceParams;
        CheckFileExists(jsonFileSourceParams!.FilePath);

        string content = string.Empty;
       
        try
        {
            // Read file content
            content = await File.ReadAllTextAsync(jsonFileSourceParams.FilePath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading file at {jsonFileSourceParams!.FilePath}: {ex.Message}", ex);
        }
        // Set the result in parameters
        jsonFileSourceParams.SetValue(content);
        return jsonFileSourceParams;
    }



    protected async override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params)
    {
        JsonFileSourceParams<TValue>? jsonFileSourceParams = @params as JsonFileSourceParams<TValue>;
        CheckFileExists(jsonFileSourceParams!.FilePath);
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

    protected async override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams @params)
    {
        // Cast the params to JsonFileSourceParams
        JsonFileSourceParams? jsonFileSourceParams = @params as JsonFileSourceParams;

        if (jsonFileSourceParams == null)
        {
            throw new ArgumentException(ExceptionMessage);
        }
        CheckFileExists(jsonFileSourceParams!.FilePath);

        try
        {
            // Get the file information
            var fileInfo = new FileInfo(jsonFileSourceParams.FilePath);

            // Get the size of the file in bytes (scalar value)
            long fileSizeInBytes = fileInfo.Length;

            // Set the scalar result (file size in bytes) as the result
            jsonFileSourceParams.SetValue(fileSizeInBytes);

            await Task.CompletedTask;
            // Return the modified params with the result
            return (BaseDataSourceParams)(object)jsonFileSourceParams;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error accessing file at {jsonFileSourceParams.FilePath}: {ex.Message}", ex);
        }
    }
}
#endregion Props
#region JsonFileSource
public partial class JsonFileSource : IDataSource
{
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteNonQuery(@params);
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        where TValue : class, new()
    {
        var sourceParams = @params as BaseDataSourceParams;
        return (TBaseDataSourceParams)await ExecuteReader<TValue>(sourceParams!);
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteReader(@params);
    }

    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
    {
        var sourceParams = @params as BaseDataSourceParams;//sfgsf
        return await ExecuteReader<TValue>(sourceParams!);
    }

    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteScalar(@params);
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
        return (JsonFileSourceParams)await ExecuteNonQuery(@params);
    }

    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(JsonFileSourceParams @params) where TValue : class, new()
    {
        return await ExecuteReader<TValue>(@params);
    }

    public async Task<JsonFileSourceParams> ExecuteReaderAsync(JsonFileSourceParams @params)
    {
        return (JsonFileSourceParams)await ExecuteReader(@params);
    }

    /// <summary>
    /// Executes a scalar operation and returns a single value.
    /// </summary>
    /// <param name="params">The parameters for the operation.</param>
    /// <returns>The parameters after the scalar operation.</returns>
    public async Task<JsonFileSourceParams> ExecuteScalarAsync(JsonFileSourceParams @params)
    {
        var sourceParams = @params as BaseDataSourceParams;
        return (JsonFileSourceParams)await ExecuteScalar(sourceParams);
    }
}
#endregion JsonFileSource<>
