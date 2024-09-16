using Microsoft.Data.SqlClient;

namespace DataAccessProvider.Interfaces;

public interface IDatabaseMSSQL : IDatabase<MSSQL, SqlParameter> { }