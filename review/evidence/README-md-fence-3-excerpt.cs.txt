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

// Example 3: Execute a query using PostgresSourceParams
var pgParams = new PostgresSourceParams
{
    Query = "SELECT id, title FROM diary LIMIT 1"
};
var pgResult = await dataSourceProvider.ExecuteReaderAsync(pgParams);

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

