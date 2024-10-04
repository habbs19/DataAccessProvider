using DataAccessProvider.DataSource;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.DataSource.Source;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Text.Json;

var serviceProvider = ConfigureServices();

// Resolve the IDataSourceProvider and use it

var dataSourceProvider = serviceProvider.GetService<IDataSourceProvider>();

/// test normal
var mssqParams1 = new MSSQLSourceParams
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
var result = await dataSourceProvider!.ExecuteReaderAsync(mssqParams1);
Console.WriteLine($"\n1:  {JsonSerializer.Serialize(result.Value)}");

/// test with type return
var mssqParams2 = new MSSQLSourceParams<Diary>
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
var result2 = await dataSourceProvider!.ExecuteReaderAsync(mssqParams2);
Console.WriteLine($"\n2:  {JsonSerializer.Serialize(result2.Value)}");

/// test with type return
var codeParams = new StaticCodeParams
{
    Content = "Hello World"
};
var result3 = await dataSourceProvider!.ExecuteReaderAsync(codeParams);
Console.WriteLine($"\n3:  {JsonSerializer.Serialize(result3.Value)}");






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
