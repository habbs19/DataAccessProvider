using DataAccessProvider.DataSource;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.DataSource.Source;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
using DataAccessProviderConsole.Classes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text.Json;

var serviceProvider = ConfigureServices();

// Resolve the IDataSourceProvider and use it

var dataSourceProvider1 = serviceProvider.GetService<IDataSourceProvider>();
var dataSourceProvider2 = serviceProvider.GetService<IDataSourceProvider<MSSQLSourceParams>>();

var mssqParams1 = new MSSQLSourceParams
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
var result1a = await dataSourceProvider1!.ExecuteReaderAsync(mssqParams1);
Console.WriteLine($"\n1a:  {JsonSerializer.Serialize(result1a.Value)}");
var result1b = await dataSourceProvider2!.ExecuteReaderAsync(mssqParams1);
Console.WriteLine($"\n1b:  {JsonSerializer.Serialize(result1b.Value)}");

/// test with type return
var mssqParams2 = new MSSQLSourceParams<Diary>
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
var result2a = await dataSourceProvider1!.ExecuteReaderAsync(mssqParams2);
Console.WriteLine($"\n2a:  {JsonSerializer.Serialize(result2a.Value)}");
var result2b = await dataSourceProvider2!.ExecuteReaderAsync<Diary>(mssqParams1);
Console.WriteLine($"\n2b:  {JsonSerializer.Serialize(result2b.Value)}");


/// test with type return
var codeParams = new StaticCodeParams
{
    Content = "Hello World"
};
var result3 = await dataSourceProvider1!.ExecuteReaderAsync(codeParams);
Console.WriteLine($"\n3:  {JsonSerializer.Serialize(result3.Value)}");

var jsonFileParams = new JsonFileSourceParams<List<Movie>>
{
    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "Movie.json"),
};
var result4 = await dataSourceProvider1!.ExecuteReaderAsync(jsonFileParams);
Console.WriteLine($"\n3:  {JsonSerializer.Serialize(result4.Value.First().First().Genre)}");








static ServiceProvider ConfigureServices()
{
    string sqlString = "Server=HABIB;Database=HS;Trusted_Connection=Yes;TrustServerCertificate=Yes";
    string postgresString = "";
    string mySqlString = "";

    var services = new ServiceCollection();

    // Register necessary services
    services.AddSingleton<IDataSourceProvider, DataSourceProvider>();
    services.AddSingleton(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
    services.AddSingleton<IDataSourceFactory, DataSourceFactory>();

    // Add database source services
    services.AddScoped<IDataSource<MSSQLSourceParams>, MSSQLSource>(provider => new MSSQLSource(sqlString));
    services.AddScoped<IDataSource<PostgresSourceParams>, PostgresSource>(provider => new PostgresSource(postgresString));
    services.AddScoped<IDataSource<MySQLSourceParams>, MySQLSource>((factory) => new MySQLSource(mySqlString));
    
    services.AddScoped(factory => new PostgresSource(postgresString));
    services.AddScoped(factory => new MySQLSource(mySqlString));
    services.AddScoped(factory => new MSSQLSource(sqlString));
    services.AddScoped<JsonFileSource>();
    services.AddScoped<StaticCodeSource>();

    services.AddScoped<IDataSource, PostgresSource>();
    services.AddScoped<IDataSource, OracleDataSource>();
    services.AddScoped<IDataSource, MongoDBSource>();
    services.AddScoped<IDataSource, MySQLSource>();


    return services.BuildServiceProvider();
}

class Diary
{
    public int DiaryID { get; set; }
    public string UserID { get; set; }
    public string Category { get; set; }
    public DateTime Date { get; set; }
    public string Content { get; set; }
}
