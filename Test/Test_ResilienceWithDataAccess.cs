using System.Data;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Resilience;
using DataAccessProvider.MSSQL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Test;

[TestClass]
public class Test_ResilienceWithDataAccess
{
    [TestMethod]
    public async Task ExecuteAsync_RetriesTransientFailures_ThenSucceeds()
    {
        // Arrange
        var policy = new BasicResiliencePolicy(maxRetries: 3, perAttemptTimeout: TimeSpan.FromSeconds(1));

        var mssqlParams = new MSSQLSourceParams
        {
            Query = "INSERT INTO Users (Name) VALUES (@Name)",
            CommandType = CommandType.Text,
            Timeout = 30
        };
        mssqlParams.AddParameter("@Name", SqlDbType.NVarChar, "John Doe");

        var mockDataSource = new Mock<IDataSource>(MockBehavior.Strict);

        // Simulate two transient failures, then a success
        var callSequence = 0;
        mockDataSource
            .Setup(ds => ds.ExecuteNonQueryAsync(It.IsAny<MSSQLSourceParams>()))
            .Returns((MSSQLSourceParams _) =>
            {
                callSequence++;

                if (callSequence <= 2)
                {
                    throw new InvalidOperationException($"Simulated transient failure #{callSequence}");
                }

                // Simulate successful result
                var successfulParams = new MSSQLSourceParams
                {
                    Query = mssqlParams.Query,
                    CommandType = mssqlParams.CommandType,
                    Timeout = mssqlParams.Timeout
                };
                successfulParams.AddParameter("@Name", SqlDbType.NVarChar, "John Doe");
                successfulParams.AffectedRows = 1;

                return Task.FromResult(successfulParams);
            });

        // Act
        var result = await policy.ExecuteAsync(ct =>
        {
            // Note: ExecuteNonQueryAsync has no CancellationToken parameter in this project
            return mockDataSource.Object.ExecuteNonQueryAsync(mssqlParams);
        });

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.AffectedRows);
        Assert.AreEqual(3, callSequence); // 2 failures + 1 success

        mockDataSource.Verify(
            ds => ds.ExecuteNonQueryAsync(It.IsAny<MSSQLSourceParams>()),
            Times.Exactly(3));
    }
}