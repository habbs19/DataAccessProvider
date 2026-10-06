var mySqlParams = new MySQLSourceParams
{
    Query = "SELECT * FROM mytable",
    Timeout = TimeSpan.FromSeconds(30), // Override default timeout
    RetryCount = 5, // Override default retry count
    CircuitBreakerThreshold = 0.5 // Override circuit breaker threshold
};

var result = await dataSourceProvider.ExecuteReaderAsync(mySqlParams);

