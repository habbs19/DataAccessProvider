using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
using System.Data.Common;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DataAccessProvider.DataSource.Source
{
    #region JsonFileSource
    public partial class JsonFileSource : IDataSource
    {
        /// <summary>
        /// Executes a non-query operation for a parameterized data source.
        /// </summary>
        /// <typeparam name="TBaseDataSourceParams">The base type for data source parameters.</typeparam>
        /// <param name="params">The data source parameters.</param>
        /// <returns>The parameters after execution.</returns>
        public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            JsonFileSourceParams? jsonFileSourceParams = @params as JsonFileSourceParams;

            if (jsonFileSourceParams != null)
            {
                return (TBaseDataSourceParams)(object)await ExecuteNonQueryAsync(jsonFileSourceParams);
            }
            else
            {
                throw new InvalidCastException($"The provided parameter is not of type JsonFileSourceParams.");
            }
        }

        /// <summary>
        /// Reads content from the JSON file and deserializes it to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JSON content into.</typeparam>
        /// <typeparam name="TBaseDataSourceParams">The base type for data source parameters.</typeparam>
        /// <param name="params">The data source parameters including file path.</param>
        /// <returns>The parameters after reading the file.</returns>
        public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TBaseDataSourceParams : BaseDataSourceParams<TValue>
            where TValue : class, new()
        {
            JsonFileSourceParams<TValue>? jsonFileSourceParams = @params as JsonFileSourceParams<TValue>;

            try
            {

                // Read file content
                string content = await File.ReadAllTextAsync(jsonFileSourceParams!.FilePath);

                var result = JsonSerializer.Deserialize<TValue>(content)!;

                // Set the result in parameters
                @params.SetValue(result);

                return @params;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading file at {jsonFileSourceParams!.FilePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads content from the JSON file.
        /// </summary>
        /// <typeparam name="TBaseDataSourceParams">The base type for data source parameters.</typeparam>
        /// <param name="params">The parameters including file path.</param>
        /// <returns>The parameters after reading the file.</returns>
        public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            JsonFileSourceParams? jsonFileSourceParams = @params as JsonFileSourceParams;

            if (jsonFileSourceParams != null)
            {
                return (TBaseDataSourceParams)(object)await ExecuteReaderAsync(jsonFileSourceParams);
            }
            else
            {
                throw new InvalidCastException($"The provided parameter is not of type JsonFileSourceParams.");
            }
        }

        /// <summary>
        /// Executes a scalar operation and returns a single value.
        /// </summary>
        /// <typeparam name="TBaseDataSourceParams">The base type for data source parameters.</typeparam>
        /// <param name="params">The data source parameters.</param>
        /// <returns>The parameters after the scalar operation.</returns>
        public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            JsonFileSourceParams? jsonFileSourceParams = @params as JsonFileSourceParams;

            if (jsonFileSourceParams != null)
            {
                return (TBaseDataSourceParams)(object)await ExecuteScalarAsync(jsonFileSourceParams);
            }
            else
            {
                throw new InvalidCastException($"The provided parameter is not of type JsonFileSourceParams.");
            }
        }

        public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
        {
            return await ExecuteReaderAsync<TValue, BaseDataSourceParams<TValue>>(@params);
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
                string content = @params.Content;

                // Write content to the file (overwriting any existing content)
                await File.WriteAllTextAsync(@params.FilePath, content);

                // Set the value to the number of bytes written
                @params.SetValue(content.Length);

                return @params;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing to file at {@params.FilePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads content from the JSON file and deserializes it to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JSON content into.</typeparam>
        /// <param name="params">The parameters including file path.</param>
        /// <returns>The parameters after reading the file.</returns>
        public async Task<JsonFileSourceParams> ExecuteReaderAsync<TValue>(JsonFileSourceParams @params) 
            where TValue : class, new()
        {
            var sourceParams = @params as BaseDataSourceParams<TValue>;
            var result = await ExecuteReaderAsync<TValue>(sourceParams!);
            return (JsonFileSourceParams)(object)result;
        }

        /// <summary>
        /// Reads content from the JSON file.
        /// </summary>
        /// <param name="params">The parameters including file path.</param>
        /// <returns>The parameters after reading the file.</returns>
        public async Task<JsonFileSourceParams> ExecuteReaderAsync(JsonFileSourceParams @params)
        {
            try
            {
                // Read file content
                string content = await File.ReadAllTextAsync(@params.FilePath);

                // Set the content as the value in parameters
                @params.SetValue(content);

                return @params;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading file at {@params.FilePath}: {ex.Message}", ex);
            }
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
}
