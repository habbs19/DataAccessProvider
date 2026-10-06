using System.Data;
using System.Reflection;
using System.Text.Json;
using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Types;
#if MSSQL
using DataAccessProvider.MSSQL;
using ProviderSource = DataAccessProvider.MSSQL.MSSQLSource;
using ProviderParams = DataAccessProvider.MSSQL.MSSQLSourceParams;
using TypedParams = DataAccessProvider.MSSQL.MSSQLSourceParams<Row>;
#elif POSTGRES
using DataAccessProvider.Postgres;
using ProviderSource = DataAccessProvider.Postgres.PostgresSource;
using ProviderParams = DataAccessProvider.Postgres.PostgresSourceParams;
using TypedParams = DataAccessProvider.Postgres.PostgresSourceParams<Row>;
#elif MYSQL
using DataAccessProvider.MySql;
using ProviderSource = DataAccessProvider.MySql.MySQLSource;
using ProviderParams = DataAccessProvider.MySql.MySQLSourceParams;
using TypedParams = DataAccessProvider.MySql.MySQLSourceParams<Row>;
#elif ORACLE
using DataAccessProvider.Oracle;
using ProviderSource = DataAccessProvider.Oracle.OracleSource;
using ProviderParams = DataAccessProvider.Oracle.OracleSourceParams;
using TypedParams = DataAccessProvider.Oracle.OracleSourceParams<Row>;
#elif SNOWFLAKE
using DataAccessProvider.Snowflake;
using ProviderSource = DataAccessProvider.Snowflake.SnowflakeSource;
using ProviderParams = DataAccessProvider.Snowflake.SnowflakeSourceParams;
using TypedParams = DataAccessProvider.Snowflake.SnowflakeSourceParams<Row>;
#elif MONGO
using DataAccessProvider.MongoDB;
#endif

var events = new List<object>();
void Record(string name, object? detail) => events.Add(new { name, detail });
#if MONGO
var source = new MongoDBSource("mongodb://127.0.0.1:1/review?serverSelectionTimeoutMS=500");
Record("construction", source.GetType().FullName);
#else
var source = new ProviderSource(DummyConnection());
using (var connection = source.GetConnection())
    Record("driverConstruction", new { type = connection.GetType().FullName, assembly = connection.GetType().Assembly.GetName().Version?.ToString() });
#endif
Record("coreAssembly", typeof(DataAccessParameter).Assembly.GetName().Version?.ToString());
#if REGISTRATION && (MSSQL || POSTGRES || MYSQL || MONGO)
var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
RegistrationContract.Register(services);
Record("registeredServices", services.Count);
#endif
foreach (var assembly in new[] { source.GetType().Assembly, typeof(DataAccessParameter).Assembly }.Distinct())
{
    try
    {
        var api = assembly.GetExportedTypes().OrderBy(t => t.FullName).Select(t => new {
            type = t.FullName,
            properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Select(p => p.ToString()).Order().ToArray(),
            methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName).Select(m => m.ToString()).Order().ToArray(),
            constructors = t.GetConstructors().Select(c => c.ToString()).Order().ToArray()
        }).ToArray();
        Record("publicApi", new { assembly = assembly.GetName().Name, api });
    }
    catch (Exception ex) { Record("publicApiError", new { assembly = assembly.GetName().Name, error = ex.ToString() }); }
}
Console.WriteLine(JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true }));

#if !MONGO
static string DummyConnection()
{
#if MSSQL
    return "Server=127.0.0.1,1;Database=review;Integrated Security=true;Encrypt=false;Connect Timeout=1";
#elif POSTGRES
    return "Host=127.0.0.1;Port=1;Database=review;Username=review;Password=review;Timeout=1";
#elif MYSQL
    return "Server=127.0.0.1;Port=1;Database=review;User ID=review;Password=review;Connection Timeout=1";
#elif ORACLE
    return "User Id=review;Password=review;Data Source=127.0.0.1:1/FREEPDB1";
#else
    return "account=review;user=review;password=review;db=review;schema=PUBLIC";
#endif
}
#endif
public sealed class Row { public int Id { get; set; } public string? Name { get; set; } }

#if REGISTRATION && (MSSQL || POSTGRES || MYSQL || MONGO)
public static class RegistrationContract
{
    public static void Register(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
    {
#if MSSQL
        services.AddDataAccessProviderMSSQL("Server=localhost");
#elif POSTGRES
        services.AddDataAccessProviderPostgres("Host=localhost");
#elif MYSQL
        services.AddDataAccessProviderMySql("Server=localhost");
#else
        services.AddDataAccessProviderMongoDB("mongodb://localhost/review");
#endif
    }
}
#endif
