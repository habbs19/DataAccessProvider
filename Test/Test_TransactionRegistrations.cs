using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using DataAccessProvider.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace Test;

[TestClass]
public class Test_TransactionRegistrations
{
    [TestMethod]
    public void MSSQL_RegistersTransactionProviderAsSameScopedSource()
    {
        var services = new ServiceCollection();
        services.AddDataAccessProviderMSSQL("Server=.;Database=master;");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<MSSQLSource>();

        Assert.AreSame(source, scope.ServiceProvider.GetRequiredService<IDataSource<MSSQLSourceParams>>());
        Assert.AreSame(source, scope.ServiceProvider.GetRequiredService<IDatabaseTransactionProvider<MSSQLSourceParams>>());
    }

    [TestMethod]
    public void Postgres_RegistersTransactionProviderAsSameScopedSource()
    {
        var services = new ServiceCollection();
        services.AddDataAccessProviderPostgres("Host=localhost;Database=test;");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<PostgresSource>();

        Assert.AreSame(source, scope.ServiceProvider.GetRequiredService<IDataSource<PostgresSourceParams>>());
        Assert.AreSame(source, scope.ServiceProvider.GetRequiredService<IDatabaseTransactionProvider<PostgresSourceParams>>());
    }

    [TestMethod]
    public void MySQL_RegistersTransactionProviderAsSameScopedSource()
    {
        var services = new ServiceCollection();
        services.AddDataAccessProviderMySql("Server=localhost;Database=test;");

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var source = scope.ServiceProvider.GetRequiredService<MySQLSource>();

        Assert.AreSame(source, scope.ServiceProvider.GetRequiredService<IDataSource<MySQLSourceParams>>());
        Assert.AreSame(source, scope.ServiceProvider.GetRequiredService<IDatabaseTransactionProvider<MySQLSourceParams>>());
    }
}
