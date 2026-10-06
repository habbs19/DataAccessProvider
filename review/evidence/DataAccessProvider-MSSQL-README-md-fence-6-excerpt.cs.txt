public class Example
{
    public void AddParameters()
    {
        // For SQL Server
        var parameters = new List<SqlParameter>();
        parameters.AddParameter("@Id", DataAccessDbType.Int32, 1);
        parameters.AddParameter("@Id", DataAccessDbType.Int32, 2);

        // Use parameters with your database command
    }
}

