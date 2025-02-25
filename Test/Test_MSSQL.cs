using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.MSSQL;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Data;

namespace Test;
[TestClass]
public class Test_MSSQL
{
    private string _testConnectionString = string.Empty;
    private readonly Mock<IDataSourceFactory> _dataSourceFactoryMock;

    [TestInitialize]
    public void Setup()
    {
        // Load the configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)  // Set the base path for config file location
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // Retrieve the connection string from appsettings.json
        _testConnectionString = configuration.GetConnectionString("TestConnection");

        // Initialize the MSSQLDatabase with a test connection string
    }

    public Test_MSSQL()
    {
        // Mocking the IDataSourceFactory
        _dataSourceFactoryMock = new Mock<IDataSourceFactory>();
    }
    [TestMethod]
    public async Task MSSQLProvider()
    {
        // Arrange
        var mssqlParams = new MSSQLSourceParams
        {
            Query = "INSERT INTO Users (Name) VALUES ('John Doe')",
            CommandType = CommandType.Text,
            Timeout = 30
        };
        mssqlParams.AddParameter("@Name", SqlDbType.NVarChar, "John Doe");

        // Mock the CreateDataSource method to return a mock IDataSource
        var mockDataSource = new Mock<IDataSource>();
        mockDataSource
            .Setup(ds => ds.ExecuteNonQueryAsync(It.IsAny<MSSQLSourceParams>()))
            .ReturnsAsync(mssqlParams);  // Simulate the return value

        _dataSourceFactoryMock
            .Setup(f => f.CreateDataSource(mssqlParams))
            .Returns(mockDataSource.Object);

        // Act
        var result = await mockDataSource.Object.ExecuteNonQueryAsync(mssqlParams);

        // Assert
        Assert.AreEqual(mssqlParams, result);
        Assert.AreEqual(1, result.AffectedRows); // Assuming the query affects 1 row
    }

}
