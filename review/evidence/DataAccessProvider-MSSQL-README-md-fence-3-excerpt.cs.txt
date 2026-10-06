// Resolve the IDataSourceProvider from the service provider
var dataSourceProvider = serviceProvider.GetService<IDataSourceProvider>();

// Example 1: Execute a query using MSSQLSourceParams
var mssqlParams1 = new MSSQLSourceParams
{
    Query = "SELECT TOP 1 * FROM [dbo].[Diary]"
};
var result1 = await dataSourceProvider.ExecuteReaderAsync(mssqlParams1);

// Example 2: Execute a query with a typed return (e.g., Diary class)
var mssqlParams2 = new MSSQLSourceParams<Diary>
{
    Query = "SELECT TOP 1 * FROM [dbo].[Diary]"
};
var result2 = await dataSourceProvider.ExecuteReaderAsync(mssqlParams2);

