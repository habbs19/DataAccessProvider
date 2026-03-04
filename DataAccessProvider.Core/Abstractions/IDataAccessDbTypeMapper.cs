using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core.Abstractions;

public interface IDataAccessDbTypeMapper
{
    object Map(DataAccessDbType dbType);
}
