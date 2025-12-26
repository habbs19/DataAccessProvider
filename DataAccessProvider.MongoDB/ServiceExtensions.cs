using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.Core.DataSource;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataAccessProvider.MongoDB;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataAccessProviderMongoDB(this IServiceCollection service, IConfiguration configuration)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        // Add database source service
        string connectionString = configuration.GetConnectionString(nameof(MongoDBSource)) ?? "";

        service.AddScoped<IDataSource<MongoDBParams>>(sp =>
        {
            var policy = sp.GetService<IResiliencePolicy>();
            return new MongoDBSource(connectionString, policy);
        });
        service.AddScoped(sp =>
        {
            var policy = sp.GetService<IResiliencePolicy>();
            return new MongoDBSource(connectionString, policy);
        });

        return service;
    }

    public static IServiceCollection AddDataAccessProviderMongoDB(this IServiceCollection service, string connectionString)
    {
        // Register necessary services
        service.TryAddScoped<IDataSourceProvider, DataSourceProvider>();
        service.TryAddScoped(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
        service.TryAddSingleton<IDataSourceFactory, DataSourceFactory>();

        service.AddScoped<IDataSource<MongoDBParams>>(sp =>
        {
            var policy = sp.GetService<IResiliencePolicy>();
            return new MongoDBSource(connectionString, policy);
        });
        service.AddScoped(sp =>
        {
            var policy = sp.GetService<IResiliencePolicy>();
            return new MongoDBSource(connectionString, policy);
        });

        return service;
    }

    /// <summary>
    /// Registers the MongoDB data source with the IDataSourceFactory.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <returns>The service provider.</returns>
    public static IServiceProvider UseDataAccessProviderMongoDB(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDataSourceFactory>();
        factory.RegisterDataSource<MongoDBParams, MongoDBSource>();
        return provider;
    }
}
