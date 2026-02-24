using DataAccessProvider.Core.Abstractions;

namespace DataAccessProvider.Oracle;

public class OracleSourceParams : BaseDatabaseSourceParams
{
    internal static IDataAccessDbTypeMapper DbTypeMapper { get; } = OracleDbTypeMapper.Instance;
}

public class OracleSourceParams<TValue> : BaseDatabaseSourceParams<TValue> where TValue : class
{
    internal static IDataAccessDbTypeMapper DbTypeMapper { get; } = OracleDbTypeMapper.Instance;
}
