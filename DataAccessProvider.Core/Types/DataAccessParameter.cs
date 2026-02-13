namespace DataAccessProvider.Core.Types;

public sealed class DataAccessParameter
{
    public string ParameterName { get; set; } = string.Empty;
    public DataAccessDbType DbType { get; set; }
    public object? Value { get; set; }
    public DataAccessParameterDirection Direction { get; set; } = DataAccessParameterDirection.Input;
    public int Size { get; set; } = -1;
}
