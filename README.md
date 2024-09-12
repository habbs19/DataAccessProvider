# Multi-Database Access Library with Generic Implementation for MSSQL, Postgres, MySQL, and Oracle

This library provides a flexible, generic solution for accessing multiple types of relational databases, including MSSQL, Postgres, MySQL, and Oracle, using a unified interface. The goal of this project is to allow seamless integration with different database engines, enabling developers to switch between databases with minimal code changes.

## Key Features

- **Generic Database Interface**: A single `IDatabase<TDatabaseType>` interface provides methods for executing queries and commands on various database systems.
- **Support for Multiple Databases**: Includes built-in support for MSSQL, Postgres, MySQL, and Oracle, with the ability to add more databases as needed.
- **Dependency Injection Ready**: Easily integrates with .NET Core Dependency Injection (DI), enabling you to register and resolve different database services for use in your applications.
- **Command Execution**: Supports both `ExecuteReaderAsync` for fetching data and `ExecuteNonQueryAsync` for executing non-query commands (e.g., inserts, updates).
- **Flexible Factory Pattern**: Uses a `DatabaseFactory` to create the appropriate database service based on the database type (MSSQL, Postgres, etc.).
- **Fallback Support**: MySQL and Oracle databases can be configured to share a common fallback implementation, with clear error handling for unsupported databases.

## How to Use

1. **Install the Library**: Add this library as a dependency to your project (instructions vary based on package management system).

2. **Register Database Services**: In your `.NET` application, register the required database type in the `Startup.cs` or `Program.cs` file using Dependency Injection.

    ```csharp
    services.AddSingleton<DatabaseFactory>(sp => new DatabaseFactory(Configuration.GetConnectionString("DefaultConnection")));
    services.AddScoped<IDatabase<MSSQL>, MSSQLDatabase>();
    services.AddScoped<IDatabase<Postgres>, PostgresDatabase>();
    ```

3. **Inject Database into Services**: Inject the desired `IDatabase` implementation into your service classes or controllers to perform database operations.

    ```csharp
    public class UserRepository : IUserRepository
    {
        private readonly IDatabase<MSSQL> _mssqlDatabase;

        public UserRepository(IDatabase<MSSQL> mssqlDatabase)
        {
            _mssqlDatabase = mssqlDatabase;
        }

        public async Task<object> GetUsersByRoleAsync(string roleName)
        {
            // Define the stored procedure and parameters
            string storedProcedure = "sp_GetUsersByRole";
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@RoleName", SqlDbType.NVarChar) { Value = roleName }
            };

            // Execute the stored procedure
            var result = await _mssqlDatabase.ExecuteReaderAsync(storedProcedure, parameters);

            return result;
        }
    }
    ```

## Supported Databases

- **MSSQL**: Microsoft SQL Server
- **Postgres**: PostgreSQL
- **MySQL**: MySQL and MariaDB
- **Oracle**: Oracle Database (shared fallback with MySQL)

## Contributions

Contributions are welcome! If you'd like to add support for additional databases or improve the library, feel free to open a pull request.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE.txt) file for details.
