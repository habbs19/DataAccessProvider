using DataAccessProviderConsole.Demos;
using DataAccessProviderConsole.Setup;
await using var host = ServiceConfiguration.ConfigureServices();
Console.WriteLine("DataAccessProvider 1.4: use the modern clients and one selected provider package.");
if (args.Contains("--database")) await DataAccessDemo.RunAsync(host);
