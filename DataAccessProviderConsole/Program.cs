using DataAccessProviderConsole.Demos;
using DataAccessProviderConsole.Setup;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = ServiceConfiguration.ConfigureServices();

ServiceConfiguration.ConfigureProviders(serviceProvider);

await ResilienceDemo.RunAsync();
await DataAccessDemo.RunAsync(serviceProvider);
