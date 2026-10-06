// In Startup.cs or Program.cs
services.AddDataAccessProviderMongoDB(configuration);

// Or with a direct connection string
services.AddDataAccessProviderMongoDB("mongodb://localhost:27017/myDatabase");

// Register the MongoDB data source with the factory
serviceProvider.UseDataAccessProviderMongoDB();

