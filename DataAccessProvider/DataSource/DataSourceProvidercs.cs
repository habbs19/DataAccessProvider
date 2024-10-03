using Amazon.Auth.AccessControlPolicy;
using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using static MongoDB.Driver.WriteConcern;

namespace DataAccessProvider.DataSource
{

    #region DataSourceProvider
    /// <summary>
    /// Provides methods to interact with different data sources using a factory to create appropriate sources (e.g., MSSQL, PostgreSQL).
    /// </summary>
    public class DataSourceProvider : IDataSourceProvider
    {
        private readonly IDataSourceFactory _sourceFactory;

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
        public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            IDataSource dataSource = GetDataSourceByParams<TBaseDataSourceParams>(_sourceFactory)!;
            return await dataSource.ExecuteNonQueryAsync(@params);
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
            IDataSource dataSource = GetDataSourceByParams<TValue,TBaseDataSourceParams>(_sourceFactory)!;
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
            IDataSource dataSource = GetDataSourceByParams<TBaseDataSourceParams>(_sourceFactory)!;
            return await dataSource.ExecuteReaderAsync(@params);
        }

        public Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
        {
            IDataSource dataSource = null!;
            switch (@params.GetType().Name)
            {
                case nameof(MSSQLSourceParams):
                    dataSource = _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.MSSQL);
                    break;
                case nameof(JsonFileSourceParams):
                    dataSource = _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.JsonFile);
                    break;
                case nameof(PostgresSourceParams):
                    dataSource = _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.PostgreSQL);
                    break;
                default:
                    throw new ArgumentException("Unsupported data source type for ExecuteReaderAsync");
            }
            return dataSource.ExecuteReaderAsync(@params);
        }

        /// <summary>
        /// Executes a scalar query asynchronously and returns a single value (e.g., an aggregate result like COUNT).
        /// </summary>
        /// <typeparam name="TBaseDataSourceParams">The base data source parameters.</typeparam>
        /// <param name="params">The parameters containing the query and necessary details for execution.</param>
        /// <returns>The same parameter object after execution, with the scalar result stored in it.</returns>
        public async Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            IDataSource dataSource = GetDataSourceByParams<TBaseDataSourceParams>(_sourceFactory)!;
            return await dataSource.ExecuteScalarAsync(@params);
        }

        /// <summary>
        /// Creates a database command for the given query and connection.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="connection">The database connection to use.</param>
        /// <returns>The <see cref="DbCommand"/> ready for execution.</returns>
        public DbCommand GetCommand(string query, DbConnection connection)
        {
            IDataSource dataSource = GetDataSourceByParams<TBaseDataSourceParams>(_sourceFactory)!;
            return dataSource.GetCommand(query, connection);
        }

        /// <summary>
        /// Gets the database connection object.
        /// </summary>
        /// <returns>The <see cref="DbConnection"/> object representing the database connection.</returns>
        public DbConnection GetConnection()
        {
            throw new NotImplementedException();
        }
        private static IDataSource GetDataSourceByParams<TBaseDataSourceParams>(IDataSourceFactory _sourceFactory) where TBaseDataSourceParams : BaseDataSourceParams
        {
            // Use switch to select the appropriate IDataSource based on TBaseDataSourceParams
            switch (typeof(TBaseDataSourceParams).Name)
            {
                case nameof(MSSQLSourceParams):
                    return _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.MSSQL);

                case nameof(JsonFileSourceParams):
                    return _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.JsonFile);

                case nameof(PostgresSourceParams):
                    return _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.PostgreSQL);

                default:
                    throw new ArgumentException("Unsupported data source type");
            }
        }

        private static IDataSource GetDataSourceByParams<TValue,TBaseDataSourceParams>(IDataSourceFactory _sourceFactory) where TBaseDataSourceParams : BaseDataSourceParams<TValue> where TValue : class
        {
            // Use switch to select the appropriate IDataSource based on TBaseDataSourceParams
            switch (typeof(TBaseDataSourceParams).Name)
            {
                case nameof(MSSQLSourceParams):
                    return _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.MSSQL);

                case nameof(JsonFileSourceParams):
                    return _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.JsonFile);

                case nameof(PostgresSourceParams):
                    return _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.PostgreSQL);

                default:
                    throw new ArgumentException("Unsupported data source type");
            }
        }
    }

   
    #endregion DataSourceProvider
    # region DataSourceProvider<TBaseDataSourceParams>
    /// <summary>
    /// Generic data source provider class for executing commands against various data sources.
    /// </summary>
    /// <typeparam name="TBaseDataSourceParams">The type of base data source parameters.</typeparam>
    public class DataSourceProvider<TBaseDataSourceParams> : IDataSourceProvider<TBaseDataSourceParams> where TBaseDataSourceParams : BaseDataSourceParams
    {
        private readonly IDataSourceFactory _sourceFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSourceProvider{TBaseDataSourceParams}"/> class.
        /// </summary>
        /// <param name="sourceFactory">The factory responsible for creating data source instances.</param>
        public DataSourceProvider(IDataSourceFactory sourceFactory)
        {
            _sourceFactory = sourceFactory;
        }

        /// <summary>
        /// Executes a non-query SQL command (e.g., INSERT, UPDATE, DELETE) asynchronously.
        /// </summary>
        /// <param name="params">The parameters containing the SQL command and other necessary details.</param>
        /// <returns>The same parameter object after execution, potentially updated with affected rows or other details.</returns>
        public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync(TBaseDataSourceParams @params)
        {
            IDataSource<TBaseDataSourceParams> dataSource = null!;

            // Determine the appropriate data source based on the parameter type
            switch (typeof(TBaseDataSourceParams).Name)
            {
                case nameof(MSSQLSourceParams):
                    {
                        dataSource = _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.MSSQL) as IDataSource<TBaseDataSourceParams>;
                        break;
                    }
                case nameof(JsonFileSourceParams):
                    {
                        dataSource = _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.JsonFile) as IDataSource<TBaseDataSourceParams>;
                        break;
                    }
                case nameof(PostgresSourceParams):
                    {
                        dataSource = _sourceFactory.CreateDataSource(Types.DataSourceTypeEnum.PostgreSQL) as IDataSource<TBaseDataSourceParams>;
                        break;
                    }
                default:
                    throw new ArgumentException("Unsupported data source type");
            }

            if (dataSource == null)
            {
                throw new InvalidOperationException("Failed to create data source.");
            }

            // Execute the non-query operation on the selected data source
            return await dataSource.ExecuteNonQueryAsync(@params);
        }

        /// <summary>
        /// Executes a query asynchronously and reads the result set as objects of type <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The type to map the result set to.</typeparam>
        /// <param name="params">The parameters containing the query and necessary details for execution.</param>
        /// <returns>The same parameter object after execution, with the result stored in it.</returns>
        public Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue>(TBaseDataSourceParams @params) where TValue : class, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes a query asynchronously and returns a dictionary where the column names are the keys and the values are the row values.
        /// </summary>
        /// <param name="params">The parameters containing the query and necessary details for execution.</param>
        /// <returns>The same parameter object after execution, with the result stored in it.</returns>
        public Task<TBaseDataSourceParams> ExecuteReaderAsync(TBaseDataSourceParams @params)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes a scalar query asynchronously and returns a single value (e.g., an aggregate result like COUNT).
        /// </summary>
        /// <param name="params">The parameters containing the query and necessary details for execution.</param>
        /// <returns>The same parameter object after execution, with the scalar result stored in it.</returns>
        public Task<TBaseDataSourceParams> ExecuteScalarAsync(TBaseDataSourceParams @params)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a database command for the given query and connection.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="connection">The database connection to use.</param>
        /// <returns>The <see cref="DbCommand"/> ready for execution.</returns>
        public DbCommand GetCommand(string query, DbConnection connection)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the database connection object.
        /// </summary>
        /// <returns>The <see cref="DbConnection"/> object representing the database connection.</returns>
        public DbConnection GetConnection()
        {
            throw new NotImplementedException();
        }
    }
    # endregion DataSourceProvider<TBaseDataSourceParams>
}
