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
}

public partial class StaticCodeSource : IDataSource<StaticCodeParams>
{
}