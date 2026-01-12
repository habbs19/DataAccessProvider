using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Types;
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

    public Test_MSSQL()
    {
        _dataSourceFactoryMock = new Mock<IDataSourceFactory>(MockBehavior.Strict);
    }

    [TestInitialize]
    public void Setup()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        _testConnectionString = configuration.GetConnectionString("TestConnection");
    }

    [TestMethod]
    public async Task MSSQLProvider()
    {
        // Arrange
        var mssqlParams = new MSSQLSourceParams
        {
            Query = "INSERT INTO Users (Name) VALUES (@Name)",
            CommandType = CommandType.Text,
            Timeout = 30
        };
        mssqlParams.AddParameter("@Name", DataAccessDbType.String, "John Doe");

        var mockDataSource = new Mock<IDataSource>(MockBehavior.Strict);

        mockDataSource
            .Setup(ds => ds.ExecuteNonQueryAsync(It.IsAny<MSSQLSourceParams>()))
            .ReturnsAsync((MSSQLSourceParams p) =>
            {
                p.AffectedRows = 1;
                p.SetValue(1);
                return p;
            });

        _dataSourceFactoryMock
            .Setup(f => f.CreateDataSource(It.IsAny<MSSQLSourceParams>()))
            .Returns(mockDataSource.Object);

        var dataSource = _dataSourceFactoryMock.Object.CreateDataSource(mssqlParams);

        // Act
        var result = await dataSource.ExecuteNonQueryAsync(mssqlParams);

        // Assert
        Assert.AreSame(mssqlParams, result);
        Assert.AreEqual(1, result.AffectedRows);

        _dataSourceFactoryMock.Verify(f => f.CreateDataSource(mssqlParams), Times.Once);
        mockDataSource.Verify(ds => ds.ExecuteNonQueryAsync(mssqlParams), Times.Once);
    }
}
