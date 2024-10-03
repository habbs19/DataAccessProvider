using DataAccessProvider.DataSource.Params;

namespace DataAccessProvider.Interfaces.Source;

public interface IPostgresSource<IPostgresDataSourceParams> : IDataSource<IPostgresDataSourceParams> 
    where IPostgresDataSourceParams : PostgresSourceParams { }

public interface IDatabasePostgres : IDataSource { }