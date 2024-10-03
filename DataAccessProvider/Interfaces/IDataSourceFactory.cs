using DataAccessProvider.Types;
using System.Data.Common;

namespace DataAccessProvider.Interfaces;

public interface IDataSourceFactory
{
    /// <summary>
    /// Creates the appropriate data source based on the provided type.
    /// </summary>
    /// <param name="sourceType">The type of data source to create (e.g., MSSQL, Postgres, File).</param>
    /// <returns>An instance of a data source that implements the IDataSource interface.</returns>
    IDatabase<IDataSourceType, TDbParameter> CreateDataSource<IDataSourceType, TDbParameter>(DataSourceTypeEnum sourceType)
        where IDataSourceType : DataSourceType
        where TDbParameter : DbParameter;
}
