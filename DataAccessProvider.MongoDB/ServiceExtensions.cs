using DataAccessProvider.Core;
using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.Extensions;
using DataAccessProvider.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
namespace DataAccessProvider.MongoDB;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderMongoDB(this IServiceCollection services, IConfiguration configuration)
        => services.AddDataAccessProviderMongoDB(configuration.GetConnectionString(nameof(MongoDBSource)) ?? string.Empty);
    public static IServiceCollection AddDataAccessProviderMongoDB(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDataAccessProviderCore();
        if (!ProviderRegistration.Ensure(services, typeof(MongoDBSource), connectionString)) return services;
        services.TryAddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.TryAddScoped(sp => new MongoDBSource(connectionString, sp.GetRequiredService<IMongoClient>()));
        services.TryAddScoped<IDataSource<MongoDBParams>>(sp => sp.GetRequiredService<MongoDBSource>());
        services.TryAddScoped(sp => new MongoDocumentClient(connectionString, sp.GetRequiredService<IMongoClient>()));
        services.TryAddScoped<IDocumentClient>(sp => sp.GetRequiredService<MongoDocumentClient>());
        services.AddSingleton(new DataSourceRegistration(typeof(MongoDBParams), typeof(MongoDBSource)));
        services.AddSingleton(new DataSourceRegistration(typeof(MongoDBParams<>), typeof(MongoDBSource)));
        return services;
    }
    public static IServiceProvider UseDataAccessProviderMongoDB(this IServiceProvider provider) => provider;
}
