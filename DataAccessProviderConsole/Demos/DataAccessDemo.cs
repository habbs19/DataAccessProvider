using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.Resilience;
using DataAccessProvider.Core.Types;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using DataAccessProviderConsole.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace DataAccessProviderConsole.Demos;

public static class DataAccessDemo
{
    public static async Task RunAsync(ServiceProvider serviceProvider)
    {
        var dataSourceProvider = serviceProvider.GetService<IDataSourceProvider>();
        var dataSourceProviderTyped = serviceProvider.GetService<IDataSourceProvider<StaticCodeParams>>();

        if (dataSourceProvider is null)
        {
            Console.WriteLine("IDataSourceProvider is not registered.");
            return;
        }

        await RunBasicQueriesAsync(dataSourceProvider);
        await RunTypedQueriesAsync(dataSourceProvider, dataSourceProviderTyped);
        await RunMySqlQueriesAsync(dataSourceProvider);
        await RunMssqlWithResiliencePolicyAsync(dataSourceProvider);
    }

    private static async Task RunBasicQueriesAsync(IDataSourceProvider dataSourceProvider)
    {
        var codeParams = new StaticCodeParams
        {
            Content = "Hello World"
        };

        var mssqParams1 = new MSSQLSourceParams
        {
            Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]",
            CommandType = System.Data.CommandType.Text
        };

        var mssqParams3 = new MSSQLSourceParams<Diary>
        {
            Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]",
            CommandType = System.Data.CommandType.Text
        };

        var result1b = await dataSourceProvider.ExecuteReaderAsync(codeParams);
        var result1c = await dataSourceProvider.ExecuteReaderAsync(mssqParams1);
        var result1d = await dataSourceProvider.ExecuteReaderAsync(mssqParams3);

        Console.WriteLine($"\n1b:  {JsonSerializer.Serialize(result1b.Value)}");
        Console.WriteLine($"\n1c:  {JsonSerializer.Serialize(result1c.Value)}");
        Console.WriteLine($"\n1d:  {JsonSerializer.Serialize(result1d.Value)}");
    }

    private static async Task RunTypedQueriesAsync(
        IDataSourceProvider dataSourceProvider,
        IDataSourceProvider<StaticCodeParams>? dataSourceProviderTyped)
    {
        var codeParams2 = new StaticCodeParams<string>
        {
            Content = "Hello World"
        };

        var mssqParams2 = new MSSQLSourceParams<Diary>
        {
            Query = "SELECT TOP 3 * FROM [HS].[dbo].[Diary]",
            CommandType = System.Data.CommandType.Text
        };

        var jsonFileParams2 = new JsonFileSourceParams
        {
            FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "Movie.json"),
        };

        var jsonFileParams3 = new JsonFileSourceParams<List<Genre>>
        {
            FilePath = Path.Combine(
                "C:\\Users\\habibs\\source\\repos\\aznV\\aznV.Infrastructure\\TestData",
                "Genres.json"),
        };

        var jsonFileParams3Result = await dataSourceProvider.ExecuteReaderAsync(jsonFileParams3);
        Console.WriteLine($"\n3:  {JsonSerializer.Serialize(jsonFileParams3Result.Value)}");

        // dataSourceProviderTyped usage is left here if you want to extend later
        _ = codeParams2;
        _ = mssqParams2;
        _ = jsonFileParams2;
        _ = dataSourceProviderTyped;
    }

    private static async Task RunMySqlQueriesAsync(IDataSourceProvider dataSourceProvider)
    {
        var myParams = new MySQLSourceParams
        {
            Query = "SP_StreamStore"
        };

        var json1 = new { Email = "hs_19@hotmail.com", OTPCode = 9839081 };

        myParams.AddJSONParams(3);
        // json1 is currently unused in original code; keep same behavior
        _ = json1;

        var myParamsResult = await dataSourceProvider.ExecuteReaderAsync(myParams);
        Console.WriteLine($"\n4:  {JsonSerializer.Serialize(myParamsResult.Value)}");

        var appuserParams = new MySQLSourceParams<AppUser>
        {
            Query = "SP_UserEmailStore"
        };

        appuserParams.AddParameter("Operation", DataAccessDbType.Int32, 1);

        var json2 = new { Email = "hs_19@hotmail.com" };
        appuserParams.Parameters.AddParameter(
            "Params",
            DataAccessDbType.Json,
            JsonSerializer.Serialize(json2));

        var appuserParamsResult = await dataSourceProvider.ExecuteReaderAsync(appuserParams);
        Console.WriteLine($"\n5:  {JsonSerializer.Serialize(appuserParamsResult.Value?.FirstOrDefault())}");
    }

    private static async Task RunMssqlWithResiliencePolicyAsync(IDataSourceProvider dataSourceProvider)
    {
        // Create a basic resilience policy (same as ResilienceDemo)
        var policy = new BasicResiliencePolicy(maxRetries: 3, perAttemptTimeout: TimeSpan.FromSeconds(1));

        var mssqlParams = new MSSQLSourceParams<Diary>
        {
            Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]",
            CommandType = System.Data.CommandType.Text
        };

        var attempt = 0;

        var result = await policy.ExecuteAsync(async ct =>
        {
            attempt++;
            Console.WriteLine($"[ResilienceDataAccess] Attempt {attempt}");

            // IDataSourceProvider does not take CancellationToken; the policy still enforces per-attempt timeout
            var response = await dataSourceProvider.ExecuteReaderAsync(mssqlParams).ConfigureAwait(false);
            return response;
        });

        Console.WriteLine($"\n[ResilienceDataAccess] Completed after {attempt} attempt(s): {JsonSerializer.Serialize(result.Value)}");
    }
}
