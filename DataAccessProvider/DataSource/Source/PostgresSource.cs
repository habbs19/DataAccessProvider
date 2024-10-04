﻿using DataAccessProvider.Abstractions;
using DataAccessProvider.DataSource.Params;
using DataAccessProvider.Interfaces;
using DataAccessProvider.Interfaces.Source;
using Npgsql;
using System.Data.Common;

namespace DataAccessProvider.DataSource.Source;

public sealed class PostgresSource : BaseDatabaseSource<PostgresSourceParams, NpgsqlParameter>,
    IDataSource,
    IDataSource<PostgresSourceParams>
{
    public PostgresSource(string connectionString) : base(connectionString) { }

    public override DbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public override DbCommand GetCommand(string query, DbConnection connection)
    {
        return new NpgsqlCommand(query, (NpgsqlConnection)connection);
    }
}