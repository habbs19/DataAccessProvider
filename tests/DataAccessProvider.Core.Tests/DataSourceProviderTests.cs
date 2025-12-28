using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessProvider.Core.Tests;

public class DataSourceProviderTests
{
    [Fact]
    public async Task ExecuteNonQueryAsync_WithStaticCodeParams_ShouldReturnUpdatedParams()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StaticCodeSource>();
        services.AddTransient<IDataSourceFactory, DataSourceFactory>();
        services.AddTransient<IDataSourceProvider, DataSourceProvider>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IDataSourceProvider>();
        var parameters = new StaticCodeParams { Content = "Test Content" };

        // Act
        var result = await provider.ExecuteNonQueryAsync(parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("NonQuery executed", result.Value?.ToString());
    }

    [Fact]
    public async Task ExecuteReaderAsync_WithStaticCodeParams_ShouldReturnContent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StaticCodeSource>();
        services.AddTransient<IDataSourceFactory, DataSourceFactory>();
        services.AddTransient<IDataSourceProvider, DataSourceProvider>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IDataSourceProvider>();
        var parameters = new StaticCodeParams { Content = "Test Content" };

        // Act
        var result = await provider.ExecuteReaderAsync(parameters);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Content", result.Value);
    }

    [Fact]
    public async Task ExecuteScalarAsync_WithStaticCodeParams_ShouldReturnContentSize()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<StaticCodeSource>();
        services.AddTransient<IDataSourceFactory, DataSourceFactory>();
        services.AddTransient<IDataSourceProvider, DataSourceProvider>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IDataSourceProvider>();
        var parameters = new StaticCodeParams { Content = "Test" };

        // Act
        var result = await provider.ExecuteScalarAsync(parameters);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<int>(result.Value);
        Assert.Equal(4, result.Value); // "Test" is 4 bytes in UTF-8
    }
}
