using DataAccessProvider.Core.Abstractions;
using DataAccessProvider.Core.Interfaces;

namespace DataAccessProvider.Core.DataSource
{

    #region DataSourceProvider
    /// <summary>
    /// Provides methods to interact with different data sources using a factory to create appropriate sources (e.g., MSSQL, PostgreSQL).
    /// </summary>
    [Obsolete("Migrate to the 1.4 client, command and result API before 2.0; see docs/migration.md.", DiagnosticId = "DAP001")]
    public class DataSourceProvider : IDataSourceProvider, ICancellableDataSource
    {
        private readonly IDataSourceFactory _sourceFactory;

        private static ICancellableDataSource Cancellable(IDataSource source) => source as ICancellableDataSource
            ?? throw new NotSupportedException("This custom source must implement ICancellableDataSource to accept cancellation.");
        public Task<TParams> ExecuteReaderAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams
            => Cancellable(_sourceFactory.CreateDataSource(p)).ExecuteReaderAsync(p, ct);
        public Task<TParams> ExecuteScalarAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams
            => Cancellable(_sourceFactory.CreateDataSource(p)).ExecuteScalarAsync(p, ct);
        public Task<TParams> ExecuteNonQueryAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams
            => Cancellable(_sourceFactory.CreateDataSource(p)).ExecuteNonQueryAsync(p, ct);
        public Task<TParams> ExecuteReaderAsync<T, TParams>(TParams p, CancellationToken ct) where T : class, new() where TParams : BaseDataSourceParams<T>
            => Cancellable(_sourceFactory.CreateDataSource(p)).ExecuteReaderAsync<T, TParams>(p, ct);
        public Task<BaseDataSourceParams<T>> ExecuteReaderAsync<T>(BaseDataSourceParams<T> p, CancellationToken ct) where T : class, new()
            => Cancellable(_sourceFactory.CreateDataSource(p)).ExecuteReaderAsync(p, ct);
        public Task<BaseDataSourceParams<T>> ExecuteReaderAsync<T>(BaseDataSourceParams p, CancellationToken ct) where T : class, new()
            => Cancellable(_sourceFactory.CreateDataSource(p)).ExecuteReaderAsync<T>(p, ct);
        public Task<bool> CheckHealthAsync<TParams>(TParams p, CancellationToken ct) where TParams : BaseDataSourceParams
            => Cancellable(_sourceFactory.CreateDataSource(p)).CheckHealthAsync(p, ct);

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceProvider"/> class.
        /// </summary>
        /// <param name="sourceFactory">The factory responsible for creating data source instances.</param>
        public DataSourceProvider(IDataSourceFactory sourceFactory)
        {
            _sourceFactory = sourceFactory;
        }

        /// <summary>
        /// Executes a non-query SQL command asynchronously, such as an INSERT, UPDATE, or DELETE operation, 
        /// using the appropriate data source determined by the provided parameters. 
        /// This method returns the same parameter object after execution, potentially updated with information 
        /// such as the number of affected rows or other relevant details.
        /// </summary>
        /// <typeparam name="TBaseDataSourceParams">
        /// The type of the base data source parameters. This type should derive from <see cref="BaseDataSourceParams"/> 
        /// and provide details such as the SQL query, parameters, and timeout settings.
        /// </typeparam>
        /// <param name="params">
        /// The parameters containing the SQL query to be executed, along with any required query parameters 
        /// and additional settings (e.g., command type, timeout). These parameters determine the specific data 
        /// source to be used (e.g., MSSQL, PostgreSQL, JSON-based file data).
        /// </param>
        /// <returns>
        /// Returns a task representing the asynchronous operation. The task result contains the same 
        /// <typeparamref name="TBaseDataSourceParams"/> object passed in, potentially updated with execution results 
        /// such as the number of rows affected by the command.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown if the data source type specified in <typeparamref name="TBaseDataSourceParams"/> is unsupported.
        /// </exception>
        public Task<bool> CheckHealthAsync()
        {
            throw new NotSupportedException("Use CheckHealthAsync(params) when health validation requires source parameters.");
        }

        public Task<bool> CheckHealthAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TBaseDataSourceParams : BaseDataSourceParams
        {
            IDataSource dataSource = _sourceFactory.CreateDataSource(@params);
            return dataSource.CheckHealthAsync(@params);
        }

        public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            IDataSource dataSource = _sourceFactory.CreateDataSource(@params);
            return await dataSource.ExecuteNonQueryAsync<TBaseDataSourceParams>(@params);
        }

        /// <summary>
        /// Executes a query and reads the result set asynchronously, mapping it to a list of objects of type <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The type to map the result set to.</typeparam>
        /// <typeparam name="TBaseDataSourceParams">The base data source parameters.</typeparam>
        /// <param name="params">The parameters containing the query and necessary details for execution.</param>
        /// <returns>The same parameter object after execution, with the results mapped to objects of type <typeparamref name="TValue"/>.</returns>
        public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TBaseDataSourceParams : BaseDataSourceParams<TValue>
            where TValue : class, new()
        {
            IDataSource dataSource = _sourceFactory.CreateDataSource<TValue>(@params);
            return await dataSource.ExecuteReaderAsync<TValue, TBaseDataSourceParams>(@params);
        }

        /// <summary>
        /// Executes a query and reads the result set asynchronously, mapping it to a dictionary where the column names are the keys and the values are the row values.
        /// </summary>
        /// <typeparam name="TBaseDataSourceParams">The base data source parameters.</typeparam>
        /// <param name="params">The parameters containing the query and necessary details for execution.</param>
        /// <returns>The same parameter object after execution, with the result stored as a dictionary.</returns>
        public async Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            IDataSource dataSource = _sourceFactory.CreateDataSource(@params);
            return await dataSource.ExecuteReaderAsync<TBaseDataSourceParams>(@params);
        }

        public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
        {
            IDataSource dataSource = _sourceFactory.CreateDataSource(@params);
            return await dataSource.ExecuteReaderAsync(@params);
        }

        /// <summary>
        /// Executes a scalar query asynchronously and returns a single value (e.g., an aggregate result like COUNT).
        /// </summary>
        /// <typeparam name="TBaseDataSourceParams">The base data source parameters.</typeparam>
        /// <param name="params">The parameters containing the query and necessary details for execution.</param>
        /// <returns>The same parameter object after execution, with the scalar result stored in it.</returns>
        public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            IDataSource dataSource = _sourceFactory.CreateDataSource(@params);
            return await dataSource.ExecuteScalarAsync<TBaseDataSourceParams>(@params);
        }
    }
    #endregion DataSourceProvider
    #region DataSourceProvider<>

    /// <summary>
    /// Generic data source provider class for executing commands against various data sources.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">The type of base data source parameters.</typeparam>
    [Obsolete("Migrate to the 1.4 client, command and result API before 2.0; see docs/migration.md.", DiagnosticId = "DAP001")]
    public class DataSourceProvider<TBaseDataSourceParams> : IDataSourceProvider<TBaseDataSourceParams>, ICancellableDataSource where TBaseDataSourceParams : BaseDataSourceParams
    {
        private readonly IDataSourceFactory _sourceFactory;
        private ICancellableDataSource Cancellable => new DataSourceProvider(_sourceFactory);
        Task<TParams> ICancellableDataSource.ExecuteReaderAsync<TParams>(TParams p,CancellationToken ct) => Cancellable.ExecuteReaderAsync(p,ct);
        Task<TParams> ICancellableDataSource.ExecuteScalarAsync<TParams>(TParams p,CancellationToken ct) => Cancellable.ExecuteScalarAsync(p,ct);
        Task<TParams> ICancellableDataSource.ExecuteNonQueryAsync<TParams>(TParams p,CancellationToken ct) => Cancellable.ExecuteNonQueryAsync(p,ct);
        Task<TParams> ICancellableDataSource.ExecuteReaderAsync<T,TParams>(TParams p,CancellationToken ct) => Cancellable.ExecuteReaderAsync<T,TParams>(p,ct);
        Task<BaseDataSourceParams<T>> ICancellableDataSource.ExecuteReaderAsync<T>(BaseDataSourceParams<T> p,CancellationToken ct) => Cancellable.ExecuteReaderAsync(p,ct);
        Task<BaseDataSourceParams<T>> ICancellableDataSource.ExecuteReaderAsync<T>(BaseDataSourceParams p,CancellationToken ct) => Cancellable.ExecuteReaderAsync<T>(p,ct);
        Task<bool> ICancellableDataSource.CheckHealthAsync<TParams>(TParams p,CancellationToken ct) => Cancellable.CheckHealthAsync(p,ct);

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceProvider{TBaseDataSourceParams}"/> class.
        /// </summary>
        /// <param name="sourceFactory">The factory responsible for creating data source instances.</param>
        public DataSourceProvider(IDataSourceFactory sourceFactory)
        {
            _sourceFactory = sourceFactory;
        }

        public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync(TBaseDataSourceParams @params)
        {
            IDataSource<TBaseDataSourceParams> dataSource = _sourceFactory.CreateDataSource<TBaseDataSourceParams>();
            return await dataSource.ExecuteNonQueryAsync(@params);
        }

        public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(TBaseDataSourceParams @params) where TValue : class, new()
        {
            IDataSource<TBaseDataSourceParams> dataSource = _sourceFactory.CreateDataSource<TBaseDataSourceParams>();
            return await dataSource.ExecuteReaderAsync<TValue>(@params);
        }

        public async Task<TBaseDataSourceParams> ExecuteReaderAsync(TBaseDataSourceParams @params)
        {
            IDataSource<TBaseDataSourceParams> dataSource = _sourceFactory.CreateDataSource<TBaseDataSourceParams>();
            return await dataSource.ExecuteReaderAsync(@params);
        }

        public async Task<TBaseDataSourceParams> ExecuteScalarAsync(TBaseDataSourceParams @params)
        {
            IDataSource<TBaseDataSourceParams> dataSource = _sourceFactory.CreateDataSource<TBaseDataSourceParams>();
            return await dataSource.ExecuteScalarAsync(@params);
        }
    }
    # endregion DataSourceProvider<>
}
