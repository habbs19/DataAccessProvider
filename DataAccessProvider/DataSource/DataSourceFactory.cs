using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.DataSource.Source;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
using DataAccessProvider.Types;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace DataAccessProvider.DataSource;

public class DataSourceFactory : IDataSourceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DataSourceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }


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
    public IDataSource CreateDataSource(BaseDataSourceParams baseDataSourceParams)
    {
        return baseDataSourceParams.GetType().Name switch
        {
            nameof(MSSQLSourceParams) => _serviceProvider.GetService<MSSQLSource>() ?? throw new InvalidOperationException("MSSQLSource not found in service provider"),
            nameof(PostgresSourceParams) => _serviceProvider.GetService<PostgresSource>() ?? throw new InvalidOperationException("PostgresSource not found in service provider"),
            nameof(JsonFileSourceParams) => _serviceProvider.GetService<JsonFileSource>() ?? throw new InvalidOperationException("JsonFileSource not found in service provider"),
            nameof(MySQLSourceParams) => _serviceProvider.GetService<MySQLSource>() ?? throw new InvalidOperationException("MySQLSource not found in service provider"),
            nameof(StaticCodeParams) => _serviceProvider.GetService<StaticCodeSource>() ?? throw new InvalidOperationException("StaticCodeSource not found in service provider"),
            nameof(OracleSourceParams) => _serviceProvider.GetService<OracleDataSource>() ?? throw new InvalidOperationException("OracleDataSource not found in service provider"),
            _ => throw new ArgumentException($"Unsupported data source type: {baseDataSourceParams.GetType().Name}")
        };
    }

    public IDataSource<T> CreateDataSource<T>() where T : BaseDataSourceParams
    {
        return typeof(T).Name switch
        {
            nameof(MSSQLSourceParams) => (IDataSource<T>)_serviceProvider.GetService<IDataSource<MSSQLSourceParams>>()!,
            nameof(PostgresSourceParams) => (IDataSource<T>)_serviceProvider.GetService<IDataSource<PostgresSourceParams>>()!,
            nameof(JsonFileSourceParams) => (IDataSource<T>)_serviceProvider.GetService<IDataSource<JsonFileSourceParams>>()!,
            nameof(MySQLSourceParams) => (IDataSource<T>)_serviceProvider.GetService<IDataSource<MySQLSourceParams>>()!,
            nameof(StaticCodeParams) => (IDataSource<T>)_serviceProvider.GetService<IDataSource<StaticCodeParams>>()!,
            nameof(OracleSourceParams) => (IDataSource<T>)_serviceProvider.GetService<IDataSource<OracleSourceParams>>()!,
            _ => throw new ArgumentException($"Unsupported data source type: {nameof(T)}")
        };
    }

    public IDataSource CreateDataSource<TValue>(BaseDataSourceParams<TValue> baseDataSourceParams) where TValue : class
    {
        // Get the actual runtime type
        var type = baseDataSourceParams.GetType();

        // Get the clean type name: if it's generic, remove the backtick notation (`1)
        var typeName = type.IsGenericType
                ? type.GetGenericTypeDefinition().Name.Split('`')[0]  // Removes the "`1" part
                : type.Name;

        return typeName switch
        {
            nameof(MSSQLSourceParams) => (IDataSource)_serviceProvider.GetService<MSSQLSource>()!,
            nameof(PostgresSourceParams) => (IDataSource)_serviceProvider.GetService<PostgresSource>()!,
            nameof(JsonFileSourceParams) => (IDataSource)_serviceProvider.GetService<JsonFileSource>()!,
            nameof(MySQLSourceParams) => (IDataSource)_serviceProvider.GetService<MySQLSource>()!,
            nameof(StaticCodeParams) => (IDataSource)_serviceProvider.GetService<StaticCodeSource>()!,
            nameof(OracleSourceParams) => (IDataSource)_serviceProvider.GetService<OracleDataSource>()!,
            _ => throw new ArgumentException($"Unsupported data source type: {typeName}")
        };
    }

    public IBaseDataSourceParams CreateParams<IBaseDataSourceParams>() where IBaseDataSourceParams : BaseDataSourceParams
    {
        throw new NotImplementedException();
    }
}

