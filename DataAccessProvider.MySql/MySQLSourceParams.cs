using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.MySql;

public class MySQLSourceParams : BaseDatabaseSourceParams
{
    internal static IDataAccessDbTypeMapper DbTypeMapper { get; } = MySqlDbTypeMapper.Instance;
}

public class MySQLSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class
{
    internal static IDataAccessDbTypeMapper DbTypeMapper { get; } = MySqlDbTypeMapper.Instance;
}
