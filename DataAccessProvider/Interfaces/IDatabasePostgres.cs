using Npgsql;

namespace DataAccessProvider.Interfaces;

public interface IDatabasePostgres : IDatabase<Postgres, NpgsqlParameter> { }
