    // Add connection strings for each database type
    services.AddDataAccessProviderMySQL(configuration);
    
    
    serviceProvider.UseDataAccessProviderMySQL();


