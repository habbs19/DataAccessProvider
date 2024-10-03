using DataAccessProvider.DataSource;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.DataSource.Source;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = ConfigureServices();

// Resolve the IDataSourceProvider and use it

var dataSourceProvider = serviceProvider.GetService<IDataSourceProvider>();
var mssqlResult = dataSourceProvider!.ExecuteNonQueryAsync(new MSSQLSourceParams());
var fileResult = dataSourceProvider.ExecuteNonQueryAsync(new JsonFileSourceParams());

var mssqlProvider = serviceProvider.GetService<IDataSourceProvider<MSSQLSourceParams>>();
var msparams = new MSSQLSourceParams<List<int>>();
//var paramsd = mssqlProvider!.ExecuteReaderAsync<List<int>>(msparams);
//var mssqlProvider = serviceProvider.GetService<IDataSourceProvider<MSSQLSourceParams>>();


if (dataSourceProvider != null)
{
    // Example of using the provider for MSSQL non-query execution
    var mssqlParams = new MSSQLSourceParams
    {
        Query = "INSERT INTO Users (Name) VALUES ('John Doe')"
    };

    await dataSourceProvider.ExecuteNonQueryAsync(mssqlParams);

    Console.WriteLine("Non-query executed successfully.");
}


static ServiceProvider ConfigureServices()
{
    // Load the configuration from appsettings.json
    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)  // Set the base path for config file location
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .Build();

    var services = new ServiceCollection();

    // Register necessary services
    services.AddSingleton<IDataSourceProvider, DataSourceProvider>();
    services.AddSingleton(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));

    services.AddSingleton<IDataSourceFactory, DataSourceFactory>();

    // Add database source services
    services.AddScoped<IMSSQLSource<MSSQLSourceParams>, MSSQLSource>(provider => new MSSQLSource(configuration.GetConnectionString("TestConnection")));
    services.AddScoped<IMSSQLSource, MSSQLSource>((factory) => new MSSQLSource(configuration.GetConnectionString("TestConnection")));
    
    services.AddScoped<IPostgresSource<PostgresSourceParams>, PostgresSource>(provider => new PostgresSource(configuration.GetConnectionString("TestConnection")));
    services.AddScoped<IMSSQLSource, MSSQLSource>((factory) => new MSSQLSource(configuration.GetConnectionString("TestConnection")));
    
    services.AddScoped<IDataSource, JsonFileSource>();
    services.AddScoped<IDataSource, MSSQLSource>();
    services.AddScoped<IDataSource, PostgresSource>();
    services.AddScoped<IDataSource, OR>();


    return services.BuildServiceProvider();
}