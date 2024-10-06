using DataAccessProvider.DataSource;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.DataSource.Source;
using DataAccessProvider.Interfaces;
using DataAccessProviderConsole.Classes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text.Json;

var serviceProvider = ConfigureServices();

// Resolve the IDataSourceProvider and use it

var dataSourceProvider1 = serviceProvider.GetService<IDataSourceProvider>();
var dataSourceProvider2 = serviceProvider.GetService<IDataSourceProvider<MSSQLSourceParams>>();

var jsonFileParams1 = new JsonFileSourceParams
{
    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "Movie.json"),
};
var codeParams1 = new StaticCodeParams
{
    Content = "Hello World"
};
var mssqParams1 = new MSSQLSourceParams
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
var result1a = await dataSourceProvider1!.ExecuteReaderAsync(jsonFileParams1);
var result1b = await dataSourceProvider1!.ExecuteReaderAsync(mssqParams1);
var result1c = await dataSourceProvider1!.ExecuteReaderAsync(codeParams1);

Console.WriteLine($"\n1a:  {JsonSerializer.Serialize(result1a.Value)}");
Console.WriteLine($"\n1b:  {JsonSerializer.Serialize(result1b.Value)}");
Console.WriteLine($"\n1c:  {JsonSerializer.Serialize(result1c.Value)}");


/// test with type return
 var codeParams2 = new StaticCodeParams<string>
{
    Content = "Hello World"
};
var mssqParams2 = new MSSQLSourceParams<Diary>
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
var jsonFileParams2 = new JsonFileSourceParams<Movie>
{
    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "Movie.json"),
};

/// test with type return


//var result4 = await dataSourceProvider1!.ExecuteReaderAsync(jsonFileParams);
//Console.WriteLine($"\n3:  {JsonSerializer.Serialize(result4.Value.First().First().Genre)}");








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
