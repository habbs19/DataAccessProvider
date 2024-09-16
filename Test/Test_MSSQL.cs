using DataAccessProvider;
using DataAccessProvider.Database;
using DataAccessProvider.Extensions;
using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks.Dataflow;

namespace Test;
[TestClass]
public class Test_MSSQL
{
    private string _testConnectionString = string.Empty;
    private IDatabaseMSSQL _mssqlDatabase;

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
        _mssqlDatabase = new MSSQLDatabase(_testConnectionString);
    }

    [TestMethod]
    public async Task ExecuteReaderAsync_ReturnsCorrectResult()
    {
        // Arrange
        var query = "SELECT * FROM dbo.Category";
        var parameters = new List<SqlParameter>();


        // Act
        var result = await _mssqlDatabase.ExecuteReaderAsync(query, parameters,CommandType.Text);

        // Cast result to List of dictionaries
        var resultList = ((Dictionary<int, List<Dictionary<string, object>>>)result)[0];

        // Assert
        Assert.IsNotNull(resultList);                      // Ensure result is not null
        Assert.IsInstanceOfType(resultList, typeof(List<Dictionary<string, object>>));  // Ensure it's the correct type
        Assert.AreEqual(4, resultList.Count);              // Ensure the list has 4 items

    }

    [TestMethod]
    public async Task ExecuteReaderAsync_ReturnsCorrectResultSet()
    {
        // Arrange
        var connectionString = "YourConnectionString";
        var mockConnection = new Mock<DbConnection>();
        var mockCommand = new Mock<DbCommand>();
        var mockReader = new Mock<DbDataReader>();

        // Setup mock command
        mockCommand.Setup(cmd => cmd.ExecuteReaderAsync()).ReturnsAsync(mockReader.Object);
        mockConnection.Setup(cmd => cmd.CreateCommand()).Returns(mockCommand.Object);

        // Setup mock reader
        mockReader.SetupSequence(r => r.Read())
                  .Returns(true)
                  .Returns(false); // Simulate that there is one row

        mockReader.Setup(r => r["Column1"]).Returns("Value1");

        var resultSet = new Dictionary<int, List<Dictionary<string, object>>>
        {
            {
                0, new List<Dictionary<string, object>> {
                    new Dictionary<string, object> { { "Column1", "Value1" } }
                }
            }
        };

        // Setup mock connection
        mockConnection.Setup(conn => conn.CreateCommand()).Returns(mockCommand.Object);
        mockConnection.Setup(conn => conn.OpenAsync(default)).Returns(Task.CompletedTask);

        var database = new MSSQLDatabase(connectionString);

        // Act
        var result = await database.ExecuteReaderAsync("SELECT * FROM TestTable");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType<Dictionary<int, List<Dictionary<string, object>>>>(result);
        Assert.Equals(resultSet, result);
    }

    [TestMethod]
    public void AddParametersExtension()
    {
        var parameters = new List<SqlParameter>();
        parameters.AddParameter("myParam1", SqlDbType.Int, 19);
        parameters.AddParameter("myParam2", SqlDbType.NChar, "Hello World!");
        var mssql = _mssqlDatabase.ExecuteNonQueryAsync("");
        
        Assert.AreEqual(2, parameters.Count);
        
    }

    [TestMethod]
    public void GetConnection_ShouldReturnSqlConnection()
    {
        // Act

        var connection = _mssqlDatabase.GetConnection();

        // Assert
        Assert.IsNotNull(connection);
        Assert.IsInstanceOfType(connection, typeof(SqlConnection));
        Assert.AreEqual(_testConnectionString, connection.ConnectionString);
    }
}
