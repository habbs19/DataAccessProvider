using DataAccessProvider.Core.Abstractions;
namespace DataAccessProvider.Core.Interfaces;
/// <summary>
/// Defines a contract for providing data sources.
/// </summary>
public interface IDataSourceProvider : IDataSource { }
/// <summary>
/// Defines a contract for providing data sources that are configured using specified parameters.
/// </summary>
/// <remarks>Implementations of this interface should supply the necessary functionality to retrieve and manage
/// data sources based on the provided parameters. This interface extends IDataSource<TDataSourceParams> to support
/// parameterized data source provisioning.</remarks>
/// <typeparam name="TDataSourceParams">The type of parameters used to configure the data source. Must inherit from BaseDataSourceParams.</typeparam>
public interface IDataSourceProvider<TDataSourceParams> : IDataSource<TDataSourceParams> 
    where TDataSourceParams : BaseDataSourceParams { }