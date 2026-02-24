using DataAccessProvider.Core.Types;

namespace DataAccessProvider.Core.Abstractions;

internal interface IDataAccessDbTypeMapper
{
    object Map(DataAccessDbType dbType);
}
