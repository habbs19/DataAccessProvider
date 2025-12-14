using DataAccessProvider.Core.Extensions;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccessProviderConsole.Setup;

public static class ServiceConfiguration
{
    public static ServiceProvider ConfigureServices()
    {
        string sqlString = "Server=HABIB;Database=HS;Trusted_Connection=Yes;TrustServerCertificate=Yes";
        string postgresString = "";
        string mySqlString = "Server=127.0.0.1;Port=3306;Database=aznv;Uid=root;Pwd=password;";

        var services = new ServiceCollection();

        services.AddDataAccessProviderCore();
        services.AddDataAccessProviderMySql(mySqlString);
        services.AddDataAccessProviderMSSQL(sqlString);
        //services.AddScoped<IDataSource, PostgresSource>();
        //services.AddScoped<IDataSource, OracleDataSource>();
        //services.AddScoped<IDataSource, MongoDBSource>();
        //services.AddScoped<IDataSource, StaticCodeSource>();

        return services.BuildServiceProvider();
    }

    public static void ConfigureProviders(ServiceProvider serviceProvider)
    {
        serviceProvider.UseDataAccessProviderMSSQL();
        serviceProvider.UseDataAccessProviderMySql();
    }
}