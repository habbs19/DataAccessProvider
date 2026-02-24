using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.MSSQL;

public class MSSQLSourceParams : BaseDatabaseSourceParams
{
    internal static IDataAccessDbTypeMapper DbTypeMapper { get; } = SqlServerDbTypeMapper.Instance;
}

public class MSSQLSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class
{
    internal static IDataAccessDbTypeMapper DbTypeMapper { get; } = SqlServerDbTypeMapper.Instance;
}
