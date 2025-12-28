using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.Interfaces;
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

    // Helper class for testing unsupported parameters
    private class TestUnsupportedParams : Abstractions.BaseDataSourceParams
    {
    }
}
