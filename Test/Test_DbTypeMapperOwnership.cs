using DataAccessProvider.Core.Types;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Test;

[TestClass]
public class Test_DbTypeMapperOwnership
{
    private const string DummySqlServerConnectionString = "Server=.;Database=master;";
    private const string DummyMySqlConnectionString = "Server=localhost;Database=test;";

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

    private static DbParameter InvokeCreateDbParameter(object source, DbCommand command, DataAccessParameter parameter)
    {
        var method = source.GetType().GetMethod("CreateDbParameter", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        return (DbParameter)method.Invoke(source, new object[] { command, parameter })!;
    }
}
