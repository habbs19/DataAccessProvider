namespace DataAccessProvider.Core.Types;

/// <summary>
/// Enum representing different types of data sources.
/// </summary>
public enum DataSourceType
{
    /// <summary>
    /// MySQL database source.
    /// </summary>
    MySQL,

    /// <summary>
    /// Microsoft SQL Server database source.
    /// </summary>
    MSSQL,

    /// <summary>
    /// PostgreSQL database source.
    /// </summary>
    Postgres,

    /// <summary>
    /// MySQL (alternative capitalization) database source.
    /// </summary>
    MySql,

    /// <summary>
    /// Oracle database source.
    /// </summary>
    Oracle,

    /// <summary>
    /// MongoDB document database source.
    /// </summary>
    MongoDB,

    /// <summary>
    /// JSON file data source.
    /// </summary>
    JsonFile,

    /// <summary>
    /// Static code data source.
    /// </summary>
    StaticCode
}
