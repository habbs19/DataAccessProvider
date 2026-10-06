using DataAccessProvider.Core.Extensions;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using DataAccessProvider.Postgres;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DataAccessProvider.Core.Interfaces;

namespace DataAccessProviderConsole.Setup;

public static class ServiceConfiguration
{
    public static ServiceProvider ConfigureServices()
    {
        // Build configuration (from appsettings.json, env vars, etc.)
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        // Read connection strings from configuration
        string? sqlString = configuration.GetConnectionString("MSSQL");
        string? postgresString = configuration.GetConnectionString("Postgres");
        string? mySqlString = configuration.GetConnectionString("MySql");

        var services = new ServiceCollection();

        // Make configuration available to the rest of the app
        services.AddSingleton(configuration);
        services.AddDataAccessProviderCore(configuration);

        // Only register providers when connection strings are present
        if (!string.IsNullOrWhiteSpace(mySqlString))
        {
            services.AddDataAccessProviderMySql(mySqlString);
        }

        if (!string.IsNullOrWhiteSpace(sqlString))
        {
            services.AddDataAccessProviderMSSQL(sqlString);
        }

        if (!string.IsNullOrWhiteSpace(postgresString))
        {
            services.AddDataAccessProviderPostgres(postgresString);
        }

        //services.AddScoped<IDataSource, PostgresSource>();
        //services.AddScoped<IDataSource, OracleDataSource>();
        //services.AddScoped<IDataSource, MongoDBSource>();
        //services.AddScoped<IDataSource, StaticCodeSource>();

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public static void ConfigureProviders(ServiceProvider serviceProvider) { /* Add-provider registration is complete; retained compatibility shim. */ }
}
