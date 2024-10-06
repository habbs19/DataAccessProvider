using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;

namespace DataAccessProvider.DataSource.Source;
#region Props
public partial class StaticCodeSource : BaseSource
{
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
}



#endregion Props

public partial class StaticCodeSource : IDataSource
{
    public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
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

    public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}

public partial class StaticCodeSource : IDataSource<StaticCodeParams>
{
    public Task<StaticCodeParams> ExecuteNonQueryAsync(StaticCodeParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(StaticCodeParams @params) where TValue : class, new()
    {
        throw new NotImplementedException();
    }

    public Task<StaticCodeParams> ExecuteReaderAsync(StaticCodeParams @params)
    {
        throw new NotImplementedException();
    }

    public Task<StaticCodeParams> ExecuteScalarAsync(StaticCodeParams @params)
    {
        throw new NotImplementedException();
    }
}