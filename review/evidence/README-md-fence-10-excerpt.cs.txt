public class Example
{
    public void AddParameters()
    {
        // For SQL Server
        var parameters = new List<SqlParameter>();
        parameters.AddParameter("@Id", DataAccessDbType.Int32, 1);
        parameters.AddParameter("@Id", DataAccessDbType.Int32, 2);

        // For PostgreSQL
        var parameters = new List<NpgsqlParameter>();
        parameters.AddParameter("@Name", DataAccessDbType.String, "John Doe");

        // Use parameters with your database command
    }
}

