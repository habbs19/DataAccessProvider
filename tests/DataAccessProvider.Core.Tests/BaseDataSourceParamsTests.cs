using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.DataSource.Params;

namespace DataAccessProvider.Core.Tests;

public class BaseDataSourceParamsTests
{
    [Fact]
    public void SetValue_WithSingleValue_ShouldSetValueAsEnumerable()
    {
        // Arrange
        var params1 = new StaticCodeParams<TestModel>();

        // Act
        params1.SetValue(new TestModel { Id = 1, Name = "Test" });

        // Assert
        Assert.NotNull(params1.Value);
        Assert.Single(params1.Value);
        Assert.Equal(1, params1.Value.First().Id);
        Assert.Equal("Test", params1.Value.First().Name);
    }

    [Fact]
    public void SetValue_WithList_ShouldSetValueAsEnumerable()
    {
        // Arrange
        var params1 = new StaticCodeParams<TestModel>();
        var list = new List<TestModel>
        {
            new TestModel { Id = 1, Name = "Test1" },
            new TestModel { Id = 2, Name = "Test2" }
        };

        // Act
        params1.SetValue(list);

        // Assert
        Assert.NotNull(params1.Value);
        Assert.Equal(2, params1.Value.Count());
        Assert.Equal(1, params1.Value.First().Id);
        Assert.Equal(2, params1.Value.Last().Id);
    }

    [Fact]
    public void SetValue_NonGeneric_ShouldSetObjectValue()
    {
        // Arrange
        var params1 = new StaticCodeParams();

        // Act
        params1.SetValue("TestValue");

        // Assert
        Assert.NotNull(params1.Value);
        Assert.Equal("TestValue", params1.Value);
    }

    [Fact]
    public void SetValue_NonGeneric_WithNull_ShouldSetNull()
    {
        // Arrange
        var params1 = new StaticCodeParams();

        // Act
        params1.SetValue(null!);

        // Assert
        Assert.Null(params1.Value);
    }

    [Fact]
    public void Value_InitiallyNull_ShouldReturnNull()
    {
        // Arrange & Act
        var params1 = new StaticCodeParams<TestModel>();

        // Assert
        Assert.Null(params1.Value);
    }

    // Helper class for testing
    private class TestModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
