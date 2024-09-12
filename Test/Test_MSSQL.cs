using DataAccessProvider.Database;
using DataAccessProvider.Interfaces;
using Microsoft.Data.SqlClient;
using Moq;
using System.Data.Common;

namespace Test;
[TestClass]
public class Test_MSSQL
{
    private Mock<DbConnection> _mockDbConnection;
    private Mock<DbCommand> _mockDbCommand;
    private Mock<DbDataReader> _mockDbDataReader;

    //[TestInitialize]
    public void Setup()
    {
        _mockDbConnection = new Mock<DbConnection>();
        _mockDbCommand = new Mock<DbCommand>();
        _mockDbDataReader = new Mock<DbDataReader>();

        // Setup the command and connection behavior
        //_mockDbCommand.Setup(c => c.ExecuteReaderAsync());
       // _mockDbConnection.Setup(c => c.CreateCommand()).Returns(_mockDbCommand.Object);
    }

    [TestMethod]
    public async Task ExecuteReaderAsync_ReturnsCorrectResult()
    {
        _mockDbConnection = new Mock<DbConnection>();
        _mockDbCommand = new Mock<DbCommand>();
        _mockDbDataReader = new Mock<DbDataReader>();

        // Arrange
        var testDatabase = new MSSQLDatabase("myConnectionString");
        var query = "SELECT * FROM Test";
        var parameters = new List<DbParameter>();
        var expectedResult = new Dictionary<int, List<Dictionary<string, object>>>
        {
            {
                0, new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "Column1", "Value1" }, { "Column2", 1 } }
                }
            }
        };

        // Setup mock data reader behavior
        _mockDbDataReader.SetupSequence(r => r.ReadAsync(It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(true)  // Simulate a row being read
            .ReturnsAsync(false); // End of result set

        _mockDbDataReader.Setup(r => r.GetName(0)).Returns("Column1");
        _mockDbDataReader.Setup(r => r.GetValue(0)).Returns("Value1");
        _mockDbDataReader.Setup(r => r.GetName(1)).Returns("Column2");
        _mockDbDataReader.Setup(r => r.GetValue(1)).Returns(1);
        _mockDbDataReader.Setup(r => r.FieldCount).Returns(2);

        // Act
        var result = await testDatabase.ExecuteReaderAsync(query, parameters);

        // Assert
        Assert.IsNotNull(result);
        var resultSet = result as Dictionary<int, List<Dictionary<string, object>>>;
        Assert.AreEqual(expectedResult.Count, resultSet.Count);
        Assert.AreEqual(expectedResult[0][0]["Column1"], resultSet[0][0]["Column1"]);
        Assert.AreEqual(expectedResult[0][0]["Column2"], resultSet[0][0]["Column2"]);
    }
}
