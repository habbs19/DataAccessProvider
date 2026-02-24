using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Postgres;

public class PostgresSourceParams : BaseDatabaseSourceParams
{
    internal static IDataAccessDbTypeMapper DbTypeMapper { get; } = PostgresDbTypeMapper.Instance;
}

public class PostgresSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class
{
    internal static IDataAccessDbTypeMapper DbTypeMapper { get; } = PostgresDbTypeMapper.Instance;
}
