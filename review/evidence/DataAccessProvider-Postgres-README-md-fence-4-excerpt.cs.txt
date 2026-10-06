var dataSourceProvider = serviceProvider.GetRequiredService<IDataSourceProvider>();

var queryParams = new PostgresSourceParams
{
    Query = "SELECT id, name FROM users WHERE id = @id",
    Parameters = new List<NpgsqlParameter>
    {
        new("@id", 1)
    }
};

var result = await dataSourceProvider.ExecuteReaderAsync(queryParams);

