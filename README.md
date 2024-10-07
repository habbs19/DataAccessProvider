# Data Access Provider Framework

The **Data Access Provider Framework** offers a flexible, pluggable way to interact with various data sources such as SQL databases (MSSQL, PostgreSQL, MySQL), file-based sources (JSON), and non-relational databases (MongoDB). By standardizing the approach to data access with source parameters (SourceParams), the framework allows developers to seamlessly switch between different data sources with minimal code changes.

## Table of Contents

- [Data Access Provider Framework](#data-access-provider-framework)
- [Features](#features)
- [Supported Data Sources](#supported-data-sources)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [How It Works](#how-it-works)
- [Connection Strings in appsettings.json](#connection-strings-in-appsettingsjson)
- [Example Usage of IDataSourceProvider](#example-usage-of-idatasourceprovider)
- [Registering a Custom Data Source](#registering-a-custom-data-source)
  - [Method Definition](#method-definition)
  - [Step 1: Create a Custom SourceParams Class](#step-1-create-a-custom-sourceparams-class)
  - [Step 2: Implement a Custom IDataSource](#step-2-implement-a-custom-idatasource)
  - [Step 3: Register the Custom Data Source with the Factory](#step-3-register-the-custom-data-source-with-the-factory)
  - [Step 4: Use the Custom Data Source](#step-4-use-the-custom-data-source)
- [Contributions](#contributions)
- [License](#license)

## Features

- **Multi-Database Support**: Seamlessly switch between MSSQL, MySQL, PostgreSQL, MongoDB, Oracle, and other data sources with minimal code changes.
- **Asynchronous Execution**: Supports `async/await` for efficient, non-blocking database operations.
- **Parameterization & Result Mapping**: Safely execute parameterized queries and map results to objects or handle raw data.
- **Extensibility & Custom Support**: Easily extend the framework to add new data sources or register custom ones.
- **Unified API**: Standardized query execution methods across different databases.
- **Flexible Testing**: Simplifies testing against multiple data sources with minimal setup.
- **Seamless Switching**: Switch between data sources by passing appropriate parameter objects, without altering core logic.


## Supported Data Sources

- **SQL Databases**:
  - MSSQL (`MSSQLSourceParams`)
  - PostgreSQL (`PostgresSourceParams`)
  - MySQL (`MySQLSourceParams`)
  - Oracle (`OracleSourceParams`)
- **NoSQL Databases**:
  - MongoDB (`MongoDBParams`)
- **File-Based**:
  - JSON File (`JsonFileSourceParams`)
  - Static Code (`StaticCodeParams`)

You can also extend the provider to support any custom data source by registering new data source implementations.

---

## Getting Started

The **DataAccessProvider** is designed to provide a simple, consistent interface for interacting with multiple database types, such as MSSQL, MySQL, PostgreSQL, MongoDB, and more. By leveraging this provider, you can easily switch between databases by passing the appropriate data source parameters, without needing to modify your core application logic.

### Prerequisites

1. **.NET Framework or .NET Core**: Make sure you have a compatible version of .NET installed.
2. **Database Drivers**: Install the necessary database drivers based on the data sources you're using.
   - **MSSQL**: `Microsoft.Data.SqlClient`
   - **PostgreSQL**: `Npgsql`
   - **MySQL**: `MySql.Data`
   - **MongoDB**: `MongoDB.Driver`

### Installation

To get started, clone the repository and reference it in your project. You'll also need to install the required NuGet packages for your data sources.


```bash
git clone https://github.com/your-repo/dataaccessprovider.git
```

### How It Works:

1. **`RegisterDataSource<TParams, TSource>()`**: This method allows the external consumer to register new custom data source types.
2. **Custom Data Source**: You create new `BaseDataSourceParams` and `IDataSource` implementations.
3. **Service Registration**: Register the custom data source with the DI container and factory in `Startup.cs`.
4. **Usage**: Use the `IDataSourceProvider` to execute queries on the custom data source.

## Example Usage of IDataSourceProvider

### Add DataAccessProvider 

```csharp    
    // Add connection strings for each database type
    services.AddDataAccessProvider(configuration)
```

## Connection Strings in appsettings.json

To store your connection strings in the `appsettings.json` file, use the following structure:

### Example `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MSSQLSource": "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
    "PostgresSource": "Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword",
    "MySQLSource": "Server=myServerAddress;Database=myDataBase;User=myUsername;Password=myPassword;",
    "OracleSource": "Data Source=MyOracleDB;User Id=myUsername;Password=myPassword;"
  }
}
```

## Example Usage of `IDataSourceProvider`

The `IDataSourceProvider` in this framework automatically determines which data source to use based on the provided `SourceParams`. This makes it flexible to switch between different data sources like MSSQL, PostgreSQL, or even JSON files, without changing your core logic.

Additionally, when using generic types, the provider can infer the type and return results mapped to a specified class, making it easy to handle type-safe responses.

### Example Code:

```csharp
// Resolve the IDataSourceProvider from the service provider
var dataSourceProvider = serviceProvider.GetService<IDataSourceProvider>();

// Example 1: Execute a query using MSSQLSourceParams
var mssqParams1 = new MSSQLSourceParams
{
    Query = "SELECT TOP 1 * FROM [dbo].[Diary]"
};
var result1 = await dataSourceProvider.ExecuteReaderAsync(mssqParams1);

// Example 2: Execute a query with a typed return (e.g., Diary class)
var mssqParams2 = new MSSQLSourceParams<Diary>
{
    Query = "SELECT TOP 1 * FROM [dbo].[Diary]"
};
var result2 = await dataSourceProvider.ExecuteReaderAsync(mssqParams2);

// Example 3: Execute a query using StaticCodeParams (for static content)
var codeParams = new StaticCodeParams
{
    Content = "Hello World"
};
var result3 = await dataSourceProvider.ExecuteReaderAsync(codeParams);

// Example 4: Execute a query using JsonFileSourceParams
var jsonFileParams = new JsonFileSourceParams
{
    Content = @"{Name: 'Michael Jackson'}"
};
var result4 = await dataSourceProvider.ExecuteNonQueryAsync(jsonFileParams);
```

### Results
```csharp
1:  [{"Id":1,"Title":"First Entry","Date":"2022-01-01T00:00:00"}]

2:  [{"Id":1,"Title":"First Entry","Date":"2022-01-01T00:00:00"}]

3:  "Hello World"

4:  {"Name": "Michael Jackson"}
```

## Registering a Custom Data Source

The **DataAccessProvider** allows you to register your own custom data sources at runtime using the `RegisterDataSource<TParams, TSource>()` method. This enables you to extend the framework by adding support for new data source types, without modifying the existing factory.

### Method Definition

```csharp
public void RegisterDataSource<TParams, TSource>() 
    where TParams : BaseDataSourceParams
    where TSource : IDataSource;
```

### Step 1: Create a Custom `SourceParams` Class

Define a custom `SourceParams` class that extends `BaseDataSourceParams` and contains any additional properties you need.

```csharp
public class XmlFileSourceParams : BaseDataSourceParams
{
    /// <summary>
    /// Path to the XML file to be read.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// The root element in the XML file where the data begins.
    /// </summary>
    public string RootElement { get; set; }

    /// <summary>
    /// An optional XPath query to select specific nodes from the XML document.
    /// </summary>
    public string? XPathQuery { get; set; }

    /// <summary>
    /// Specifies whether to ignore namespaces in the XML document.
    /// </summary>
    public bool IgnoreNamespaces { get; set; } = true;

    /// <summary>
    /// Additional attributes or settings related to XML parsing.
    /// </summary>
    public Dictionary<string, string>? AdditionalAttributes { get; set; }
}
```

### Step 2: Implement a Custom `IDataSource`

Create a custom data source by implementing the `IDataSource` interface and providing the necessary logic to interact with the custom data source.

### Step 3: Register the Custom Data Source with the Factory

Now that the `CustomSourceParams` and `CustomDataSource` are defined, you can register them with the `DataSourceFactory` by calling `RegisterDataSource<TParams, TSource>()` from the DataSourceFactory service.

```csharp
var dataSourceFactory = serviceProvider.GetService<IDataSourceFactory>();

// Register the custom data source
dataSourceFactory.RegisterDataSource<XmlFileSourceParams, XmlFileSource>();
```

### Step 4: Use the Custom Data Source

Once the custom data source is registered, you can use it just like any other data source by passing `CustomSourceParams` to the `IDataSourceProvider`.
```csharp
var xmlFileParams = new XmlFileSourceParams
{
    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "data.xml"),
    RootElement = "Employees",
    XPathQuery = "//Employee[Age > 30]", // Optional XPath to filter nodes
    IgnoreNamespaces = true,
    AdditionalAttributes = new Dictionary<string, string>
    {
        { "attribute1", "value1" },
        { "attribute2", "value2" }
    }
};

// Use IDataSourceProvider to execute the query
var result = await dataSourceProvider.ExecuteReaderAsync(xmlFileParams);
Console.WriteLine($"\nXML Data Source Result: {JsonSerializer.Serialize(result.Value)}");
```

## `DbParameter` Extension Method

The library includes an extension method for adding database parameters to a list in a type-safe and generic manner. This method supports multiple database types, including SQL Server and PostgreSQL.

### Example Usage

Here is how you can use the `AddParameter` extension method to add parameters to a `List<DbParameter>`:

```csharp
public class Example
{
    public void AddParameters()
    {
        // For SQL Server
        var parameters = new List<SqlParameter>();
        parameters.AddParameter("@Id", DbType.Int32, 1);
        parameters.AddParameter("@Id", DbType.Int32, 2);

        // For PostgreSQL
        var parameters = new List<NpgsqlParameter>();
        parameters.AddParameter("@Name", DbType.String, "John Doe");

        // Use parameters with your database command
    }
}
```

## Contributions

Contributions are welcome! If you'd like to add support for additional databases or improve the library, feel free to open a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.