# Data Access Provider Framework

The **Data Access Provider Framework** offers a flexible, pluggable way to interact with various data sources such as SQL databases (MSSQL, PostgreSQL, MySQL), file-based sources (JSON), and non-relational databases (MongoDB). By standardizing the approach to data access with source parameters (SourceParams), the framework allows developers to seamlessly switch between different data sources with minimal code changes.

## Table of Contents

- [Features](#features)
- [Supported Data Sources](#supported-data-sources)
- [Getting Started](#getting-started)
  - [Installation](#installation)
  - [Basic Usage](#basic-usage)
- [DataSourceFactory](#datasourcefactory)
- [DataSourceProvider](#datasourceprovider)
- [API Reference](#api-reference)
  - [IDataSource](#idatasource)
  - [IDataSourceFactory](#idatasourcefactory)
  - [IDataSourceProvider](#idatasourceprovider)
  - [Data Source Params](#data-source-params)
  - [Specific Data Sources](#specific-data-sources)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Multi-Database Support**: Switch between MSSQL, MySQL, PostgreSQL, MongoDB, Oracle, and other data sources with minimal code changes.
- **Asynchronous Execution**: Use `async/await` for non-blocking database operations.
- **Parameterization**: Safely execute parameterized queries.
- **Result Mapping**: Map query results directly to objects or handle raw data as dictionaries.
- **Extensibility**: Easily extend the framework to add support for new data sources.
- **Unified API**: The framework abstracts the differences between various databases, allowing you to use a standardized set of methods to execute queries and operations, regardless of the underlying data source.
- **Flexible Testing**: With its design, the framework makes it easier to test different database interactions. By simply passing different data source parameter objects, you can test against multiple databases, file systems, or other data sources in a seamless manner.
- **Seamless Data Source Switching**: You can switch from one data source to another by providing the appropriate parameter class. For example, switch from MSSQL to PostgreSQL by passing `MSSQLSourceParams` or `PostgresSourceParams`, with no changes to the core execution logic.
- **Custom Data Source Support**: The framework allows developers to register their own custom data sources. Using the `DataSourceFactory`, you can define and register custom parameter classes and their corresponding data source implementations, extending the framework for specific use cases.


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

## Registering a Custom Data Source

The **DataAccessProvider** allows you to register your own custom data sources at runtime using the `RegisterDataSource<TParams, TSource>()` method. This enables you to extend the library by adding support for new data source types, without modifying the existing factory.

### Method Definition

```csharp
public void RegisterDataSource<TParams, TSource>() 
    where TParams : BaseDataSourceParams
    where TSource : IDataSource;
```

### How It Works:

1. **`RegisterDataSource<TParams, TSource>()`**: This method allows the external consumer to register new custom data source types.
2. **Custom Data Source**: You create new `BaseDataSourceParams` and `IDataSource` implementations.
3. **Service Registration**: Register the custom data source with the DI container and factory in `Startup.cs`.
4. **Usage**: Use the `IDataSourceProvider` to execute queries on the custom data source.

## Example Usage of IDataSourceProvider

Once you have registered your data sources and implemented the factory, you can use the `IDataSourceProvider` to execute queries and handle different data source types seamlessly.

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
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
var result1 = await dataSourceProvider.ExecuteReaderAsync(mssqParams1);
Console.WriteLine($"\n1:  {JsonSerializer.Serialize(result1.Value)}");

// Example 2: Execute a query with a typed return (e.g., Diary class)
var mssqParams2 = new MSSQLSourceParams<Diary>
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
var result2 = await dataSourceProvider.ExecuteReaderAsync(mssqParams2);
Console.WriteLine($"\n2:  {JsonSerializer.Serialize(result2.Value)}");

// Example 3: Execute a query using StaticCodeParams (for static content)
var codeParams = new StaticCodeParams
{
    Content = "Hello World"
};
var result3 = await dataSourceProvider.ExecuteReaderAsync(codeParams);
Console.WriteLine($"\n3:  {JsonSerializer.Serialize(result3.Value)}");

// Example 4: Execute a query using JsonFileSourceParams
var jsonFileParams = new JsonFileSourceParams
{
    Content = @"{Name: 'Michael Jackson'}"
};
var result4 = await dataSourceProvider.ExecuteReaderAsync(jsonFileParams);
Console.WriteLine($"\n4:  {JsonSerializer.Serialize(result4.Value)}");
```

### Results
```csharp
1:  [{"Id":1,"Title":"First Entry","Date":"2022-01-01T00:00:00"}]

2:  [{"Id":1,"Title":"First Entry","Date":"2022-01-01T00:00:00"}]

3:  "Hello World"

4:  {"Name": "Michael Jackson"}
```

### `DbParameter` Extension Method

The library includes an extension method for adding database parameters to a list in a type-safe and generic manner. This method supports multiple database types, including SQL Server and PostgreSQL.

## Example Usage

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


```bash
git clone https://github.com/your-repo/dataaccessprovider.git
