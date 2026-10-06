// Resolve the IDataSourceProvider from the service provider
var dataSourceProvider = serviceProvider.GetService<IDataSourceProvider>();

// Example 1: Execute a query using MySQLSourceParams
var mssqParams1 = new MySQLSourceParams
{
    Query = "SELECT TOP 1 * FROM [dbo].[Diary]"
};
var result1 = await dataSourceProvider.ExecuteReaderAsync(mssqParams1);

// Example 2: Execute a query with a typed return (e.g., Diary class)
var mssqParams2 = new MySQLSourceParams<Diary>
{
    Query = "SELECT TOP 1 * FROM [dbo].[Diary]"
};
var result2 = await dataSourceProvider.ExecuteReaderAsync(mssqParams2);

