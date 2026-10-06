// In Startup.cs or Program.cs
services.AddDataAccessProviderCore(configuration);
services.AddDataAccessProviderPostgres(configuration);

// After building the provider
serviceProvider.UseDataAccessProviderPostgres();

