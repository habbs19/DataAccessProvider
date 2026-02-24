using DataAccessProvider.Core.Types;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using DataAccessProvider.Oracle;
using DataAccessProvider.Postgres;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using NpgsqlTypes;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Test;

[TestClass]
public class Test_DbTypeMapperOwnership
{
    private const string DummySqlServerConnectionString = "Server=.;Database=master;";
    private const string DummyMySqlConnectionString = "Server=localhost;Database=test;";
    private const string DummyOracleConnectionString = "Data Source=localhost/XEPDB1;";
    private const string DummyPostgresConnectionString = "Host=localhost;Database=test;";

    [TestMethod]
    public void MSSQLSource_CreateDbParameter_UsesProviderMapper()
    {
        var source = new MSSQLSource(DummySqlServerConnectionString);
        var parameter = new DataAccessParameter
        {
            ParameterName = "@Name",
            DbType = DataAccessDbType.String,
            Value = "John Doe"
        };

        var dbParameter = (SqlParameter)InvokeCreateDbParameter(source, new SqlCommand(), parameter);

        Assert.AreEqual(SqlDbType.NVarChar, dbParameter.SqlDbType);
    }

    [TestMethod]
    public void MSSQLSource_CreateDbParameter_MapsSqlAliases()
    {
        var source = new MSSQLSource(DummySqlServerConnectionString);
        var parameter = new DataAccessParameter
        {
            ParameterName = "@Value",
            DbType = DataAccessDbType.NVarChar,
            Value = "John Doe"
        };

        var dbParameter = (SqlParameter)InvokeCreateDbParameter(source, new SqlCommand(), parameter);

        Assert.AreEqual(SqlDbType.NVarChar, dbParameter.SqlDbType);
    }

    [TestMethod]
    public void MySQLSource_CreateDbParameter_UsesProviderMapper()
    {
        var source = new MySQLSource(DummyMySqlConnectionString);
        var parameter = new DataAccessParameter
        {
            ParameterName = "@Name",
            DbType = DataAccessDbType.String,
            Value = "John Doe"
        };

        var dbParameter = (MySqlParameter)InvokeCreateDbParameter(source, new MySqlCommand(), parameter);

        Assert.AreEqual(MySqlDbType.VarChar, dbParameter.MySqlDbType);
    }

    [TestMethod]
    public void MySQLSource_CreateDbParameter_MapsSqlAliases()
    {
        var source = new MySQLSource(DummyMySqlConnectionString);
        var parameter = new DataAccessParameter
        {
            ParameterName = "@Value",
            DbType = DataAccessDbType.TinyInt,
            Value = 1
        };

        var dbParameter = (MySqlParameter)InvokeCreateDbParameter(source, new MySqlCommand(), parameter);

        Assert.AreEqual(MySqlDbType.Byte, dbParameter.MySqlDbType);
    }

    [TestMethod]
    public void OracleSource_CreateDbParameter_UsesProviderMapper()
    {
        var source = new OracleSource(DummyOracleConnectionString);
        var parameter = new DataAccessParameter
        {
            ParameterName = ":Name",
            DbType = DataAccessDbType.VarChar,
            Value = "John Doe",
            Size = 10
        };

        var dbParameter = (OracleParameter)InvokeCreateDbParameter(source, new OracleCommand(), parameter);

        Assert.AreEqual(OracleDbType.Varchar2, dbParameter.OracleDbType);
    }

    [TestMethod]
    public void PostgresSource_CreateDbParameter_UsesProviderMapper()
    {
        var source = new PostgresSource(DummyPostgresConnectionString);
        var parameter = new DataAccessParameter
        {
            ParameterName = "@name",
            DbType = DataAccessDbType.Text,
            Value = "John Doe"
        };

        var dbParameter = (NpgsqlParameter)InvokeCreateDbParameter(source, new NpgsqlCommand(), parameter);

        Assert.AreEqual(NpgsqlDbType.Text, dbParameter.NpgsqlDbType);
    }

    private static DbParameter InvokeCreateDbParameter(object source, DbCommand command, DataAccessParameter parameter)
    {
        var method = source.GetType().GetMethod("CreateDbParameter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        return (DbParameter)method.Invoke(source, new object[] { command, parameter })!;
    }
}
