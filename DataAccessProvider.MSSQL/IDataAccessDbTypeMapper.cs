using DataAccessProvider.Core.Types;

namespace DataAccessProvider.MSSQL;

internal interface IDataAccessDbTypeMapper
{
    object Map(DataAccessDbType dbType);
}
