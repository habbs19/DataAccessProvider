using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;

namespace DataAccessProvider.DataSource.Source;
#region Props
public partial class StaticCodeSource
{
    protected async Task<BaseDataSourceParams> ExecuteReader(BaseDataSourceParams @params)
    {

    }

    protected async Task<BaseDataSourceParams<TValue>> ExecuteReader<TValue>(BaseDataSourceParams @params) where TValue : class, new()
    {

    }
}



#endregion Props

public partial class StaticCodeSource : IDataSource
{

    /// <summary>
    /// Executes a read operation asynchronously and processes the data based on the provided data source parameters.
    /// This method is designed to handle <see cref="StaticCodeParams"/> specifically, while throwing an exception for unsupported parameter types.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">
    /// The type of the base data source parameters, which must inherit from <see cref="BaseDataSourceParams"/>.
    /// </typeparam>
    /// <param name="params">
    /// The parameters containing the query and any other relevant data for the execution. If the parameters are of type <see cref="StaticCodeParams"/>, 
    /// the method will process its <see cref="StaticCodeParams.Content"/>.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. When the task completes, it returns the same <typeparamref name="TBaseDataSourceParams"/> object, 
    /// with any updates made during the processing.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if the provided parameters are not of a supported type, specifically when they are not <see cref="StaticCodeParams"/>.
    /// </exception>
    /// <remarks>
    /// This method currently supports <see cref="StaticCodeParams"/>. If the passed parameter object is of this type, the method processes its content 
    /// and returns the same parameters after modifying its value. The method can be extended to handle other types of data source parameters.
    /// </remarks>
    public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
    {
        // Check if the parameters are of type StaticCodeParams
        if (@params is StaticCodeParams staticCodeParams)
        {
            var content = $"DataAccessLayer - Here is your content: {staticCodeParams.Content}";

            // Perform any logic required here
            await Task.CompletedTask;

            @params.SetValue(content);
            return @params;
        }

        // Handle other BaseDataSourceParams types if needed
        throw new ArgumentException("Unsupported data source parameter type.");
    }
}

public partial class StaticCodeSource : IDataSource<StaticCodeParams>
{
}