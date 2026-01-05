using System.Data;
using System.Data.Common;
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;
using Moq;

namespace DataAccessProvider.Core.Tests;

public class TransactionContextTests
{
    [Fact]
    public void TransactionContext_InitialState_IsActive()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        // Act
        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        // Assert
        Assert.True(context.IsActive);
        Assert.False(context.IsCommitted);
        Assert.False(context.IsRolledBack);
        Assert.Same(mockConnection.Object, context.Connection);
        Assert.Same(mockTransaction.Object, context.Transaction);
    }

    [Fact]
    public async Task CommitAsync_SetsIsCommittedToTrue()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        // Act
        await context.CommitAsync();

        // Assert
        Assert.True(context.IsCommitted);
        Assert.False(context.IsRolledBack);
        Assert.False(context.IsActive);
        mockTransaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_SetsIsRolledBackToTrue()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        // Act
        await context.RollbackAsync();

        // Assert
        Assert.False(context.IsCommitted);
        Assert.True(context.IsRolledBack);
        Assert.False(context.IsActive);
        mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_AfterCommit_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        await context.CommitAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync());
    }

    [Fact]
    public async Task RollbackAsync_AfterRollback_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        await context.RollbackAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.RollbackAsync());
    }

    [Fact]
    public async Task DisposeAsync_WithoutCommit_RollsBackTransaction()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        // Act
        await context.DisposeAsync();

        // Assert
        mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_AfterCommit_DoesNotRollback()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        mockTransaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        await context.CommitAsync();

        // Act
        await context.DisposeAsync();

        // Assert
        mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_OnlyDisposesOnce()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        // Act
        await context.DisposeAsync();
        await context.DisposeAsync();

        // Assert
        mockTransaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteNonQueryAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var mockConnection = new Mock<DbConnection>();
        var mockTransaction = new Mock<DbTransaction>();
        mockTransaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var mockSource = new Mock<TestDatabaseSource>("connection-string") { CallBase = true };

        var context = new TransactionContext<DbParameter>(
            mockSource.Object,
            mockConnection.Object,
            mockTransaction.Object);

        await context.DisposeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            context.ExecuteNonQueryAsync(new TestSourceParams { Query = "SELECT 1" }));
    }
}

/// <summary>
/// Test database source for unit testing purposes.
/// </summary>
public class TestDatabaseSource : BaseDatabaseSource<DbParameter>
{
    public TestDatabaseSource(string connectionString) : base(connectionString) { }

    public override DbConnection GetConnection()
    {
        throw new NotImplementedException("Test source does not support real connections");
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        throw new NotImplementedException("Test source does not support real commands");
    }
}

/// <summary>
/// Test source params for unit testing purposes.
/// </summary>
public class TestSourceParams : BaseDatabaseSourceParams<DbParameter>
{
}
