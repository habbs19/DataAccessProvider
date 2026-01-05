using System.Data;
using System.Data.Common;
using DataAccessProvider.Core.Interfaces;

namespace DataAccessProvider.Core.Abstractions;

/// <summary>
/// Transaction support for BaseDatabaseSource.
/// </summary>
public abstract partial class BaseDatabaseSource<TParameter> : ITransactionalDataSource
    where TParameter : DbParameter
{
    /// <inheritdoc />
    public async Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        return new TransactionContext<TParameter>(this, connection, transaction);
    }

    /// <inheritdoc />
    public async Task<ITransactionContext> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        var connection = GetConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var transaction = await connection.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
        return new TransactionContext<TParameter>(this, connection, transaction);
    }

    /// <summary>
    /// Executes a reader within an existing transaction context.
    /// </summary>
    internal async Task<TBaseDataSourceParams> ExecuteReaderInTransactionAsync<TValue, TBaseDataSourceParams>(
        TBaseDataSourceParams @params,
        DbConnection connection,
        DbTransaction transaction)
        where TBaseDataSourceParams : BaseDataSourceParams<TValue>
        where TValue : class, new()
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
        if (sourceParams == null)
        {
            throw new ArgumentException("Invalid source parameters type.");
        }

        using (var command = GetCommand(sourceParams.Query, connection))
        {
            command.Transaction = transaction;
            command.CommandTimeout = sourceParams.Timeout;
            command.CommandType = sourceParams.CommandType;

            if (sourceParams.Parameters != null)
            {
                command.Parameters.AddRange(sourceParams.Parameters.ToArray());
            }

            using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                var result = await MaterializeInTransactionAsync<TValue>(reader).ConfigureAwait(false);

                if (result.Count == 1)
                {
                    sourceParams.SetValue(result[0]);
                }
                else if (result.Count > 1)
                {
                    sourceParams.SetValue(result);
                }

                return (TBaseDataSourceParams)(object)sourceParams;
            }
        }
    }

    /// <summary>
    /// Executes a reader within an existing transaction context, returning dictionary results.
    /// </summary>
    internal async Task<TBaseDataSourceParams> ExecuteReaderInTransactionAsync<TBaseDataSourceParams>(
        TBaseDataSourceParams @params,
        DbConnection connection,
        DbTransaction transaction)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
        if (sourceParams == null)
        {
            throw new ArgumentException("Invalid source parameters type.");
        }

        using (var command = GetCommand(sourceParams.Query, connection))
        {
            command.Transaction = transaction;
            command.CommandTimeout = sourceParams.Timeout;
            command.CommandType = sourceParams.CommandType;

            if (sourceParams.Parameters != null)
            {
                command.Parameters.AddRange(sourceParams.Parameters.ToArray());
            }

            var resultSet = new Dictionary<int, List<Dictionary<string, object>>>();

            using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                int resultCount = 0;
                do
                {
                    resultSet[resultCount] = await ReadResultAsync(reader).ConfigureAwait(false);
                    resultCount++;
                }
                while (await reader.NextResultAsync().ConfigureAwait(false));
            }

            if (resultSet.Count == 1)
            {
                var firstResultSet = resultSet[0];

                if (firstResultSet.Count == 1)
                {
                    sourceParams.SetValue(firstResultSet[0]);
                }
                else if (firstResultSet.Count == 0)
                {
                    sourceParams.SetValue(new Dictionary<string, object>());
                }
                else
                {
                    sourceParams.SetValue(firstResultSet);
                }
            }
            else if (resultSet.Count > 1)
            {
                sourceParams.SetValue(resultSet);
            }

            return (TBaseDataSourceParams)(object)sourceParams;
        }
    }

    /// <summary>
    /// Executes a non-query within an existing transaction context.
    /// </summary>
    internal async Task<TBaseDataSourceParams> ExecuteNonQueryInTransactionAsync<TBaseDataSourceParams>(
        TBaseDataSourceParams @params,
        DbConnection connection,
        DbTransaction transaction)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
        if (sourceParams == null)
        {
            throw new ArgumentException("Invalid source parameters type.");
        }

        using (var command = GetCommand(sourceParams.Query, connection))
        {
            command.Transaction = transaction;
            command.CommandTimeout = sourceParams.Timeout;
            command.CommandType = sourceParams.CommandType;

            if (sourceParams.Parameters != null)
            {
                command.Parameters.AddRange(sourceParams.Parameters.ToArray());
            }

            var affectedRows = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            sourceParams.SetValue(affectedRows);
            sourceParams.AffectedRows = affectedRows;

            return (TBaseDataSourceParams)(object)sourceParams;
        }
    }

    /// <summary>
    /// Executes a scalar query within an existing transaction context.
    /// </summary>
    internal async Task<TBaseDataSourceParams> ExecuteScalarInTransactionAsync<TBaseDataSourceParams>(
        TBaseDataSourceParams @params,
        DbConnection connection,
        DbTransaction transaction)
        where TBaseDataSourceParams : BaseDataSourceParams
    {
        var sourceParams = @params as BaseDatabaseSourceParams<TParameter>;
        if (sourceParams == null)
        {
            throw new ArgumentException("Invalid source parameters type.");
        }

        using (var command = GetCommand(sourceParams.Query, connection))
        {
            command.Transaction = transaction;
            command.CommandTimeout = sourceParams.Timeout;
            command.CommandType = sourceParams.CommandType;

            if (sourceParams.Parameters != null)
            {
                command.Parameters.AddRange(sourceParams.Parameters.ToArray());
            }

            var result = await command.ExecuteScalarAsync().ConfigureAwait(false);
            sourceParams.SetValue(result!);

            return (TBaseDataSourceParams)(object)sourceParams;
        }
    }

    /// <summary>
    /// Materializes reader results to typed objects within a transaction context.
    /// </summary>
    private async Task<List<TValue>> MaterializeInTransactionAsync<TValue>(DbDataReader reader)
        where TValue : class, new()
    {
        var accessors = TypeAccessorCache<TValue>.GetColumnAccessors(reader);
        var result = new List<TValue>();

        if (accessors.Length == 0)
        {
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                result.Add(new TValue());
            }

            return result;
        }

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var item = new TValue();

            foreach (var accessor in accessors)
            {
                object? rawValue = reader.IsDBNull(accessor.Ordinal) ? null : reader.GetValue(accessor.Ordinal);

                if (rawValue == null)
                {
                    if (accessor.AllowNullAssignments)
                    {
                        accessor.Assign(item, null);
                    }

                    continue;
                }

                var converted = GetPropertyValue<TValue>(accessor.Property, rawValue);

                if (converted == null)
                {
                    if (accessor.AllowNullAssignments)
                    {
                        accessor.Assign(item, null);
                    }

                    continue;
                }

                accessor.Assign(item, converted);
            }

            result.Add(item);
        }

        return result;
    }
}
