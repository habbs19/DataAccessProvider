using DataAccessProvider.Core.Types;

namespace DataAccessProvider.MySql;

internal interface IDataAccessDbTypeMapper
{
    object Map(DataAccessDbType dbType);
}
