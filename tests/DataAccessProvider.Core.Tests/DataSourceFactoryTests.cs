using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessProvider.Core.Tests;

public class DataSourceFactoryTests
{
    [Fact]
    public void RegisterDataSource_ShouldRegisterStaticCodeSource()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StaticCodeSource>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DataSourceFactory(serviceProvider);

        // Act
        var registeredSources = factory.GetRegisteredDataSources();

        // Assert
        Assert.Contains(nameof(StaticCodeParams), registeredSources.Keys);
        Assert.Equal(typeof(StaticCodeSource), registeredSources[nameof(StaticCodeParams)]);
    }

    [Fact]
    public void RegisterDataSource_ShouldRegisterJsonFileSource()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<JsonFileSource>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DataSourceFactory(serviceProvider);

        // Act
        var registeredSources = factory.GetRegisteredDataSources();

        // Assert
        Assert.Contains(nameof(JsonFileSourceParams), registeredSources.Keys);
        Assert.Equal(typeof(JsonFileSource), registeredSources[nameof(JsonFileSourceParams)]);
    }

    [Fact]
    public void CreateDataSource_WithStaticCodeParams_ShouldReturnStaticCodeSource()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StaticCodeSource>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DataSourceFactory(serviceProvider);
        var parameters = new StaticCodeParams { Content = "test" };

        // Act
        var dataSource = factory.CreateDataSource(parameters);

        // Assert
        Assert.NotNull(dataSource);
        Assert.IsType<StaticCodeSource>(dataSource);
    }

    [Fact]
    public void CreateDataSource_WithUnsupportedParams_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DataSourceFactory(serviceProvider);
        var parameters = new TestUnsupportedParams();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => factory.CreateDataSource(parameters));
        Assert.Contains("Unsupported data source type", exception.Message);
    }

    [Fact]
    public void CreateDataSource_WithConventionBasedParams_ShouldResolveBySourceName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ConventionSource>();
        var serviceProvider = services.BuildServiceProvider();
        var factory = new DataSourceFactory(serviceProvider);

        // Act
        var dataSource = factory.CreateDataSource(new ConventionSourceParams());

        // Assert
        Assert.NotNull(dataSource);
        Assert.IsType<ConventionSource>(dataSource);
    }

    [Fact]
    public void GenericFactoryCreateDataSource_WithNonGenericParamsType_ShouldResolveRegisteredSource()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ConventionSource>();
        var serviceProvider = services.BuildServiceProvider();
        IDataSourceFactory factory = new DataSourceFactory(serviceProvider);
        factory.RegisterDataSource<ConventionSourceParams, ConventionSource>();

        // Act
        var dataSource = factory.CreateDataSource<ConventionSourceParams>();

        // Assert
        Assert.NotNull(dataSource);
        Assert.IsType<ConventionSource>(dataSource);
    }

    // Helper class for testing unsupported parameters
    private class TestUnsupportedParams : Abstractions.BaseDataSourceParams
    {
    }

    private class ConventionSourceParams : BaseDataSourceParams
    {
    }

    private class ConventionSource : IDataSource, IDataSource<ConventionSourceParams>
    {
        public Task<bool> CheckHealthAsync() => Task.FromResult(true);
        public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TBaseDataSourceParams : BaseDataSourceParams => Task.FromResult(true);
        public Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TBaseDataSourceParams : BaseDataSourceParams => Task.FromResult(@params);
        public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params)
            where TValue : class, new() => Task.FromResult(@params);
        public Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TBaseDataSourceParams : BaseDataSourceParams<TValue>
            where TValue : class, new() => Task.FromResult(@params);
        public Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TBaseDataSourceParams : BaseDataSourceParams => Task.FromResult(@params);
        public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TBaseDataSourceParams : BaseDataSourceParams => Task.FromResult(@params);

        Task<ConventionSourceParams> IDataSource<ConventionSourceParams>.ExecuteNonQueryAsync(ConventionSourceParams @params)
            => Task.FromResult(@params);
        Task<BaseDataSourceParams<TValue>> IDataSource<ConventionSourceParams>.ExecuteReaderAsync<TValue>(ConventionSourceParams @params)
            => Task.FromResult<BaseDataSourceParams<TValue>>(null!);
        Task<ConventionSourceParams> IDataSource<ConventionSourceParams>.ExecuteReaderAsync(ConventionSourceParams @params)
            => Task.FromResult(@params);
        Task<ConventionSourceParams> IDataSource<ConventionSourceParams>.ExecuteScalarAsync(ConventionSourceParams @params)
            => Task.FromResult(@params);
    }
}
