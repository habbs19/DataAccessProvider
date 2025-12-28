using DataAccessProvider.Core.Extensions;

namespace DataAccessProvider.Core.Tests;

public class ObjectExtensionsTests
{
    [Fact]
    public void GetValueByKey_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "TestKey", "TestValue" }
        };

        // Act
        var result = dict.GetValueByKey<string>("TestKey");

        // Assert
        Assert.Equal("TestValue", result);
    }

    [Fact]
    public void GetValueByKey_WithMissingKey_ShouldReturnDefault()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "TestKey", "TestValue" }
        };

        // Act
        var result = dict.GetValueByKey<string>("MissingKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValueByKey_WithNullValue_ShouldReturnNull()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "TestKey", null! }
        };

        // Act
        var result = dict.GetValueByKey<string>("TestKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValueByKey_WithDBNullValue_ShouldReturnNull()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "TestKey", DBNull.Value }
        };

        // Act
        var result = dict.GetValueByKey<string>("TestKey");

        // Assert - DBNull.Value is not a string, so it should return default (null)
        Assert.Null(result);
    }

    [Fact]
    public void GetValueByKey_WithWrongType_ShouldReturnDefault()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "TestKey", 123 }
        };

        // Act
        var result = dict.GetValueByKey<string>("TestKey");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetValueByKey_WithIntValue_ShouldReturnInt()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "TestKey", 42 }
        };

        // Act
        var result = dict.GetValueByKey<int>("TestKey");

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void GetValuesByKey_WithMatchingKey_ShouldReturnValues()
    {
        // Arrange
        var list = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "Name", "John" } },
            new Dictionary<string, object> { { "Name", "Jane" } },
            new Dictionary<string, object> { { "Name", "Bob" } }
        };

        // Act
        var result = list.GetValuesByKey<string>("Name");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("John", result);
        Assert.Contains("Jane", result);
        Assert.Contains("Bob", result);
    }

    [Fact]
    public void GetValuesByKey_WithMissingKey_ShouldReturnEmptyList()
    {
        // Arrange
        var list = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "Name", "John" } },
            new Dictionary<string, object> { { "Name", "Jane" } }
        };

        // Act
        var result = list.GetValuesByKey<string>("MissingKey");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetValuesByKey_WithMixedTypes_ShouldReturnOnlyMatchingTypes()
    {
        // Arrange
        var list = new List<Dictionary<string, object>>
        {
            new Dictionary<string, object> { { "Value", "string" } },
            new Dictionary<string, object> { { "Value", 123 } },
            new Dictionary<string, object> { { "Value", "another string" } }
        };

        // Act
        var result = list.GetValuesByKey<string>("Value");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("string", result);
        Assert.Contains("another string", result);
    }
}
