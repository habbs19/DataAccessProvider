using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using DataAccessProvider.Postgres;
using DataAccessProvider.TsvImporter.Models;
using DataAccessProvider.TsvImporter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataAccessProvider.TsvImporter;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== IMDb TSV Data Importer ===\n");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Setup dependency injection
        var serviceProvider = ConfigureServices(configuration);

        // Get configuration values
        var databaseType = configuration["ImportSettings:DatabaseType"] ?? "MSSQL";
        var tsvFilePath = configuration["ImportSettings:TsvFilePath"] ?? "name.basics.tsv.gz";
        var batchSize = int.Parse(configuration["ImportSettings:BatchSize"] ?? "1000");
        var maxRecords = int.Parse(configuration["ImportSettings:MaxRecords"] ?? "0");

        // Override with command line arguments if provided
        if (args.Length > 0)
        {
            tsvFilePath = args[0];
        }
        if (args.Length > 1)
        {
            databaseType = args[1];
        }
        if (args.Length > 2)
        {
            batchSize = int.Parse(args[2]);
        }

        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Database Type: {databaseType}");
        Console.WriteLine($"  TSV File: {tsvFilePath}");
        Console.WriteLine($"  Batch Size: {batchSize}");
        Console.WriteLine($"  Max Records: {(maxRecords > 0 ? maxRecords.ToString() : "Unlimited")}");
        Console.WriteLine();

        // Validate file exists
        if (!File.Exists(tsvFilePath))
        {
            Console.WriteLine($"Error: TSV file not found: {tsvFilePath}");
            Console.WriteLine("\nUsage: DataAccessProvider.TsvImporter [tsvFilePath] [databaseType] [batchSize]");
            Console.WriteLine("  tsvFilePath: Path to the TSV file (supports .tsv and .tsv.gz)");
            Console.WriteLine("  databaseType: MSSQL, MySQL, or Postgres (default: MSSQL)");
            Console.WriteLine("  batchSize: Number of records to insert per batch (default: 1000)");
            return;
        }

        try
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var dataSourceProvider = serviceProvider.GetRequiredService<IDataSourceProvider>();

            // Initialize the database importer
            var importer = new DatabaseImporter(dataSourceProvider,
                serviceProvider.GetRequiredService<ILogger<DatabaseImporter>>(),
                databaseType);

            // Create schema if it doesn't exist
            Console.WriteLine("Checking database schema...");
            var tableExists = await importer.TableExistsAsync();
            if (!tableExists)
            {
                Console.WriteLine("Creating database schema...");
                await importer.CreateSchemaAsync();
                Console.WriteLine("Schema created successfully!\n");
            }
            else
            {
                Console.WriteLine("Schema already exists.\n");
            }

            // Import data
            Console.WriteLine("Starting import...");
            var startTime = DateTime.Now;
            var totalImported = 0;
            var batch = new List<ImdbPerson>();

            await foreach (var person in TsvReader.ReadPersonsAsync(tsvFilePath))
            {
                batch.Add(person);

                if (batch.Count >= batchSize)
                {
                    var imported = await importer.ImportBatchAsync(batch);
                    totalImported += imported;
                    Console.WriteLine($"Progress: {totalImported} records imported...");
                    batch.Clear();
                }

                // Stop if max records reached
                if (maxRecords > 0 && totalImported >= maxRecords)
                {
                    break;
                }
            }

            // Import remaining records
            if (batch.Count > 0)
            {
                var imported = await importer.ImportBatchAsync(batch);
                totalImported += imported;
            }

            var duration = DateTime.Now - startTime;
            Console.WriteLine($"\n=== Import Complete ===");
            Console.WriteLine($"Total records imported: {totalImported}");
            Console.WriteLine($"Time taken: {duration.TotalSeconds:F2} seconds");
            Console.WriteLine($"Records per second: {(totalImported / duration.TotalSeconds):F2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError during import: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    static ServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Add configuration
        services.AddSingleton(configuration);

        // Add DataAccessProvider core
        services.AddDataAccessProviderCore(configuration);

        // Read connection strings
        var mssqlConnectionString = configuration.GetConnectionString("MSSQL");
        var mysqlConnectionString = configuration.GetConnectionString("MySql");
        var postgresConnectionString = configuration.GetConnectionString("Postgres");

        // Register database providers based on what's configured
        if (!string.IsNullOrWhiteSpace(mssqlConnectionString))
        {
            services.AddDataAccessProviderMSSQL(mssqlConnectionString);
        }

        if (!string.IsNullOrWhiteSpace(mysqlConnectionString))
        {
            services.AddDataAccessProviderMySql(mysqlConnectionString);
        }

        if (!string.IsNullOrWhiteSpace(postgresConnectionString))
        {
            services.AddDataAccessProviderPostgres(postgresConnectionString);
        }

        var serviceProvider = services.BuildServiceProvider();

        // Configure providers
        if (serviceProvider.GetService<IDataSource<MSSQLSourceParams>>() is not null)
        {
            serviceProvider.UseDataAccessProviderMSSQL();
        }

        if (serviceProvider.GetService<IDataSource<MySQLSourceParams>>() is not null)
        {
            serviceProvider.UseDataAccessProviderMySql();
        }

        if (serviceProvider.GetService<IDataSource<PostgresSourceParams>>() is not null)
        {
            serviceProvider.UseDataAccessProviderPostgres();
        }

        return serviceProvider;
    }
}
