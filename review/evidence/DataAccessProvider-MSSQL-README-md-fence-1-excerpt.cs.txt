    // Add connection strings for each database type
    services.AddDataAccessProviderMSSQL(configuration);

    serviceProvider.UseDataAccessProviderMySQL();

