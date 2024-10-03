using DataAccessProvider.DataSource.Params;

namespace DataAccessProvider.Interfaces;

public interface IDatabasePostgres<IPostgresDataSourceParams> : IDataSource<IPostgresDataSourceParams> where IPostgresDataSourceParams : PostgresSourceParams { }

public interface IDatabasePostgres : IDataSource { }