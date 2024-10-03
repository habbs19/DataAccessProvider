using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;

namespace DataAccessProvider.Abstractions
{
    /// <summary>
    /// Represents a base class for database sources, providing common database operations like ExecuteNonQuery, ExecuteReader, and ExecuteScalar.
    /// </summary>
    /// <typeparam name="TDatabaseSourceParams">The type of the database source parameters.</typeparam>
    public abstract class BaseDatabaseSource<TDatabaseParams, TParameter> : IDataSource<TDatabaseParams>, IDataSource
        where TDatabaseParams : DatabaseSourceParams<TParameter>
        where TParameter : class

    {
        /// <summary>
        /// The connection string used for the database connection.
        /// </summary>
        protected string _connectionString { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDatabase{TDataSourceType, TDbParameter}"/> class.
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        public BaseDatabaseSource(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets a new instance of a database connection. Must be implemented in derived classes.
        /// </summary>
        /// <returns>A <see cref="DbConnection"/> specific to the database.</returns>
        public virtual DbConnection GetConnection()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates and returns a new command object for executing queries.
        /// Must be implemented in derived classes.
        /// </summary>
        /// <param name="query">The SQL query or command text.</param>
        /// <param name="connection">The open database connection.</param>
        /// <returns>A <see cref="DbCommand"/> object for executing commands.</returns>
        public abstract DbCommand GetCommand(string query, DbConnection connection);

        public async Task<TDatabaseParams> ExecuteNonQueryAsync(TDatabaseParams @params)
        {
            using (var connection = GetConnection())
            {
                using (var command = GetCommand(@params.Query, connection))
                {
                    command.CommandTimeout = @params.Timeout;
                    command.CommandType = @params.CommandType;
                    if (@params.Parameters != null)
                        command.Parameters.AddRange(@params.Parameters.ToArray());

                    await connection.OpenAsync();
                    @params.SetValue(await command.ExecuteNonQueryAsync());
                    return @params;
                }
            }
        }

        public async Task<TBaseDataSourceParams> ExecuteNonQueryAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            using (var connection = GetConnection())
            {
                using (var command = GetCommand(mSSQLSourceParams.Query, connection))
                {
                    command.CommandTimeout = mSSQLSourceParams.Timeout;
                    command.CommandType = mSSQLSourceParams.CommandType;
                    if (mSSQLSourceParams.Parameters != null)
                        command.Parameters.AddRange(mSSQLSourceParams.Parameters.ToArray());

                    await connection.OpenAsync();
                    @params.SetValue(await command.ExecuteNonQueryAsync());
                    return @params;
                }
            }
        }

            public Task<TDatabaseParams> ExecuteReaderAsync<T>(TDatabaseParams @params) where T : class, new()
        {
            throw new NotImplementedException();
        }

        public Task<TDatabaseParams> ExecuteReaderAsync(TDatabaseParams @params)
        {
            throw new NotImplementedException();
        }

        public Task<TBaseDataSourceParams> ExecuteReaderAsync<TValue, TBaseDataSourceParams>(TBaseDataSourceParams @params)
            where TValue : class, new()
            where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        {
            throw new NotImplementedException();
        }

        public Task<TBaseDataSourceParams> ExecuteReaderAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            throw new NotImplementedException();
        }

        public Task<TDatabaseParams> ExecuteScalarAsync(TDatabaseParams @params)
        {
            throw new NotImplementedException();
        }

        public Task<TBaseDataSourceParams> ExecuteScalarAsync<TBaseDataSourceParams>(TBaseDataSourceParams @params) where TBaseDataSourceParams : BaseDataSourceParams
        {
            throw new NotImplementedException();
        }

        public async Task<BaseDataSourceParams<TValue>> ExecuteReaderAsync<TValue>(BaseDataSourceParams<TValue> @params) where TValue : class, new()
        {
            TValue result = new();
            if (@params.GetType().Name == nameof(MSSQLSourceParams))
            {
                Console.WriteLine("name");
            }

            return null;
        }
    }
}
