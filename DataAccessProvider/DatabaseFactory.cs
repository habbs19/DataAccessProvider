using DataAccessProvider.Database;
using DataAccessProvider.Interfaces;

namespace DataAccessProvider
{
    public interface IDatabaseFactory
    {
        IDatabase<TDatabaseType> CreateDatabase<TDatabaseType>() where TDatabaseType : DatabaseType, new();
    }

    public class DatabaseFactory : IDatabaseFactory
    {
        private readonly string _connectionString;

        public DatabaseFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDatabase<TDatabaseType> CreateDatabase<TDatabaseType>() where TDatabaseType : DatabaseType, new()
        {
            return typeof(TDatabaseType) switch
            {
                Type t when t == typeof(MSSQL) => (IDatabase<TDatabaseType>)new MSSQLDatabase(_connectionString),
                Type t when t == typeof(Postgres) => (IDatabase<TDatabaseType>)new PostgresDatabase(_connectionString),
                Type t when t == typeof(MySql) || t == typeof(Oracle) => throw new NotSupportedException($"Database type {typeof(TDatabaseType).Name} is not implemented."),
                _ => throw new NotSupportedException($"Database type {typeof(TDatabaseType).Name} is not supported")
            };
        }
    }

}
