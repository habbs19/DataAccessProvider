public record User(int Id, string Name);

var typedParams = new PostgresSourceParams<User>
{
    Query = "SELECT id, name FROM users"
};

var users = await dataSourceProvider.ExecuteReaderAsync(typedParams);

