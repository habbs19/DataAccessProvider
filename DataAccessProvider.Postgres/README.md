# DataAccessProvider.Postgres

A PostgreSQL provider for the DataAccessProvider framework, built on top of the Npgsql driver. It offers async operations, parameterized queries, resilience support, and easy DI registration.

## Installation

```bash
dotnet add package DataAccessProvider.Postgres
```

## Configuration

Add a connection string named `PostgresSource` to your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "PostgresSource": "Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword"
  }
}
```

## Dependency Injection

```csharp
// In Startup.cs or Program.cs
services.AddDataAccessProviderCore(configuration);
services.AddDataAccessProviderPostgres(configuration);

// After building the provider
serviceProvider.UseDataAccessProviderPostgres();
```

You can also register the provider with a raw connection string:

```csharp
services.AddDataAccessProviderPostgres("Host=localhost;Port=5432;Database=mydb;Username=myuser;Password=mypassword");
```

## Usage

```csharp
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
```

### Typed Results

```csharp
public record User(int Id, string Name);

var typedParams = new PostgresSourceParams<User>
{
    Query = "SELECT id, name FROM users"
};

var users = await dataSourceProvider.ExecuteReaderAsync(typedParams);
```

## Resilience

If you register an `IResiliencePolicy` (e.g., via `AddDataAccessProviderCore`), the provider will automatically apply retries/timeouts around database calls.
