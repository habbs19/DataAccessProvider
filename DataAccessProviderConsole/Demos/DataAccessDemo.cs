using DataAccessProvider.Core;
using DataAccessProvider.Core.Types;
using DataAccessProvider.MSSQL;
using Microsoft.Extensions.DependencyInjection;
namespace DataAccessProviderConsole.Demos;

public static class DataAccessDemo
{
    public static async Task RunAsync(IServiceProvider host)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await using var scope = host.CreateAsyncScope();
        var database = scope.ServiceProvider.GetService<IDatabaseClient<SqlServer>>();
        if (database is null) { Console.WriteLine("Configure MSSQL to run this database example."); return; }
        var value = await database.ScalarAsync<int>(DatabaseCommand.Text("SELECT @value").WithParameter("@value", DataAccessDbType.Int32, 42), cancellation.Token);
        Console.WriteLine($"State={value.State}; value={value.Value}");
    }
}
