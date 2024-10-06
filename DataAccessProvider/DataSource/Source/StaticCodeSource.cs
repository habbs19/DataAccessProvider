using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using System.Text;

namespace DataAccessProvider.DataSource.Source;
#region Props
public partial class StaticCodeSource : BaseSource
{
    protected async override Task<BaseDataSourceParams> ExecuteNonQuery(BaseDataSourceParams @params)
    {
        // Check if the parameters are of type StaticCodeParams
        if (@params is StaticCodeParams staticCodeParams)
        {
            // Perform logic related to non-query operations on StaticCodeParams
            var content = staticCodeParams.Content;

            var updatedContent = $"{content} - NonQuery executed";

            // Simulate asynchronous work
            await Task.CompletedTask;

            // Set the updated content as the result
            staticCodeParams.SetValue(updatedContent);

            return staticCodeParams;
        }

        // Handle other types of BaseDataSourceParams if needed
        throw new ArgumentException("Unsupported data source parameter type.");
    }

    protected async override Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {
        // Check if the parameters are of type StaticCodeParams
        if (@params is StaticCodeParams staticCodeParams)
        {
            var content = $"{staticCodeParams.Content}";

            // Perform any logic required here
            await Task.CompletedTask;

            staticCodeParams.SetValue(content);
            return staticCodeParams;
        }

        // Handle other BaseDataSourceParams types if needed
        throw new ArgumentException("Unsupported data source parameter type.");
    }

    protected async override Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params)
    {
        // Check if the parameters are of type StaticCodeParams
        if (@params is StaticCodeParams<TValue> staticCodeParams)
        {
            TValue content = (TValue)Convert.ChangeType(staticCodeParams.Content, typeof(TValue));

            // Perform any logic required here
            await Task.CompletedTask;

            staticCodeParams.SetValue(content);
            return staticCodeParams;
        }

        // Handle other BaseDataSourceParams types if needed
        throw new ArgumentException("Unsupported data source parameter type.");
    }

    protected async override Task<BaseDataSourceParams> ExecuteScalar(BaseDataSourceParams @params)
    {
        // Cast the params to StaticCodeParams<TValue>
        StaticCodeParams? staticCodeParams = @params as StaticCodeParams;

        if (staticCodeParams == null)
        {
            throw new ArgumentException("Invalid parameter type. Expected StaticCodeParams.");
        }

        try
        {
            // Calculate the size of the content in bytes using UTF-8 encoding
            int contentSizeInBytes = Encoding.UTF8.GetByteCount(staticCodeParams.Content.ToString()!);

            // Set the scalar result (content size in bytes)
            staticCodeParams.SetValue(contentSizeInBytes);

            await Task.CompletedTask;

            // Return the modified params with the result
            return (BaseDataSourceParams)(object)staticCodeParams;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error processing content: {ex.Message}", ex);
        }
    }
}



#endregion Props

public partial class StaticCodeSource : IDataSource
{
    public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteNonQuery(@params);
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
        where TValue : class, new()
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
    {
        var sourceParams = @params as BaseDataSourceParams;
        return (TBaseDataSourceParams)(object)await ExecuteReader<TValue>(sourceParams!);
    }

    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteReader(@params);
    }

    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
    {
        var sourceParams = @params as BaseDataSourceParams;
        return await ExecuteReader<TValue>(sourceParams!);
    }

    public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        return (TBaseDataSourceParams)await ExecuteScalar(@params);
    }
}

public partial class StaticCodeSource : IDataSource<StaticCodeParams>
{
    public async Task<StaticCodeParams> ExecuteNonQueryAsync(StaticCodeParams @params)
    {
        return (StaticCodeParams)await ExecuteNonQuery(@params);
    }

    public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(StaticCodeParams @params) where TValue : class, new()
    {
        var sourceParams = @params as BaseDataSourceParams;
        return await ExecuteReader<TValue>(sourceParams);
    }

    public async Task<StaticCodeParams> ExecuteReaderAsync(StaticCodeParams @params)
    {
        return (StaticCodeParams)await ExecuteReader(@params);
    }

    public async Task<StaticCodeParams> ExecuteScalarAsync(StaticCodeParams @params)
    {
        return (StaticCodeParams)await ExecuteScalar(@params);
    }
}