using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Types;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace DataAccessProvider.Core.Interfaces;

public interface IDataSourceFactory
{
    /// <summary>
    /// Allows external consumers to add their own custom data source mappings.
    /// </summary>
    /// 
    public void RegisterDataSource<TParams, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TSource>() 
        where TParams : BaseDataSourceParams
        where TSource : IDataSource;

    /// <summary>
    /// Factory method for creating an appropriate data source based on the type of the provided <see cref="BaseDataSourceParams"/>.
    /// The method inspects the runtime type of the <paramref name="baseDataSourceParams"/> and returns a corresponding <see cref="IDataSource"/> implementation.
    /// </summary>
    /// <param name="baseDataSourceParams">
    /// The base data source parameters object, which determines the type of data source to create. 
    /// This object contains query details, execution parameters, and other relevant information for the database interaction.
    /// </param>
    /// <returns>
    /// An implementation of <see cref="IDataSource"/> that corresponds to the runtime type of <paramref name="baseDataSourceParams"/>.
    /// For example, if the provided object is of type <see cref="MSSQLSourceParams"/>, an instance of <see cref="MSSQLSource"/> will be returned.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the type of <paramref name="baseDataSourceParams"/> is not supported, indicating an invalid or unsupported data source type.
    /// </exception>
    /// <remarks>
    /// This method uses a <c>switch</c> expression to match the type of <paramref name="baseDataSourceParams"/> with the corresponding data source type.
    /// The data source instance is resolved from the service provider (<see cref="_serviceProvider"/>), which is expected to have all supported data sources registered.
    /// </remarks>
    IDataSource CreateDataSource(BaseDataSourceParams baseDataSourceParams);
    IDataSource CreateDataSource<TValue>(BaseDataSourceParams<TValue> baseDataSourceParams) 
        where TValue : class;

    IDataSource<TBaseDataSourceParams> CreateDataSource<TBaseDataSourceParams>() where TBaseDataSourceParams : BaseDataSourceParams;

    IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() 
        where IBaseDataSourceParams : BaseDataSourceParams;
}
