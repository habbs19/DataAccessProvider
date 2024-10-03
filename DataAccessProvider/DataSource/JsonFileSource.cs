using DataAccessProvider.Interfaces;
using MongoDB.Bson.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataAccessProvider.DataSource
{
    public class JsonFileSource : IJsonFileSource
    {
        /// <summary>
        /// Asynchronously writes content to a file or performs a non-query file operation (e.g., write or delete).
        /// </summary>
        /// <param name="filePath">The path to the file where the content will be written.</param>
        /// <param name="parameters">Optional parameters for the operation. For file operations, this could be used for passing content or instructions.</param>
        /// <param name="commandType">The type of command. Not applicable in the file context, but included for interface compatibility.</param>
        /// <param name="timeout">The time in seconds to wait before the operation times out. Not applicable for file operations, but included for interface compatibility.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of bytes written to the file.</returns>
        /// <exception cref="Exception">Thrown when an error occurs while writing to the file.</exception>
        public async Task<int> ExecuteNonQueryAsync(string filePath, List<DbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45)
        {
            try
            {
                // Example content to write to the file (from parameters or hardcoded for simplicity)
                string content = parameters != null && parameters.Any()!
                    ? parameters.First().Value!.ToString()! // Assuming content is passed via parameters
                    : "Default file content";

                // Write content to the file (overwriting any existing content)
                await File.WriteAllTextAsync(filePath, content);

                // Return the number of bytes written
                return content.Length;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing to file at {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads a JSON file from the given file path and deserializes it into a list of objects of type T.
        /// </summary>
        /// <typeparam name="T">The type of objects to deserialize from the JSON content.</typeparam>
        /// <param name="filePath">The path to the JSON file.</param>
        /// <param name="parameters">Unused in this implementation, present for interface compatibility.</param>
        /// <param name="commandType">Unused in this implementation, present for interface compatibility.</param>
        /// <param name="timeout">Unused in this implementation, present for interface compatibility.</param>
        /// <returns>A task that represents the asynchronous operation, containing a list of objects of type T.</returns>
        public async Task<List<T>> ExecuteReaderAsync<T>(string filePath, List<DbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45) where T : class, new()
        {
            try
            {
                // Read the content from the file asynchronously
                var fileContent = await File.ReadAllTextAsync(filePath);

                // Deserialize the JSON content into a list of objects of type T
                var result = JsonSerializer.Deserialize<List<T>>(fileContent);

                // Return the deserialized result
                return result ?? new List<T>();
            }
            catch (Exception ex)
            {
                // Handle any exceptions (e.g., file not found, deserialization errors)
                throw new Exception($"Error reading or deserializing JSON file at {filePath}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reads a JSON file from the given file path and returns the raw file content as a string.
        /// </summary>
        /// <param name="filePath">The path to the JSON file.</param>
        /// <param name="parameters">Unused in this implementation, present for interface compatibility.</param>
        /// <param name="commandType">Unused in this implementation, present for interface compatibility.</param>
        /// <param name="timeout">Unused in this implementation, present for interface compatibility.</param>
        /// <returns>A task that represents the asynchronous operation, containing the file content as a string.</returns>
        public async Task<object> ExecuteReaderAsync(string filePath, List<DbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45)
        {
            try
            {
                // Read the content from the file asynchronously
                var fileContent = await File.ReadAllTextAsync(filePath);

                // Return the file content as a string
                return fileContent;
            }
            catch (Exception ex)
            {
                // Handle any exceptions (e.g., file not found)
                throw new Exception($"Error reading file at {filePath}: {ex.Message}", ex);
            }
        }
        /// <summary>
        /// Asynchronously retrieves a scalar value from a file. In this implementation, it returns the size of the file in bytes.
        /// </summary>
        /// <param name="filePath">The path to the file from which the scalar value (file size) will be retrieved.</param>
        /// <param name="parameters">Optional parameters for the method. Not applicable in the file context, but included for interface compatibility.</param>
        /// <param name="commandType">The type of command. Not applicable in the file context, but included for interface compatibility.</param>
        /// <param name="timeout">The time in seconds to wait before the operation times out. Not applicable for file operations, but included for interface compatibility.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the file size in bytes as an object.</returns>
        /// <exception cref="Exception">Thrown when an error occurs while accessing or retrieving file information.</exception>
        public async Task<object> ExecuteScalarAsync(string filePath, List<DbParameter>? parameters = null, CommandType commandType = CommandType.StoredProcedure, int timeout = 45)
        {
            try
            {
                // Get file info
                var fileInfo = new FileInfo(filePath);

                // Return the file size (in bytes) as a scalar value
                return await Task.FromResult(fileInfo.Length);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving file info from {filePath}: {ex.Message}", ex);
            }
        }


        public DbCommand GetCommand(string filePath, DbConnection connection)
        {
            throw new NotImplementedException();
        }

        public DbConnection GetConnection()
        {
            throw new NotImplementedException();
        }
    }
}
