var xmlFileParams = new XmlFileSourceParams
{
    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "data.xml"),
    RootElement = "Employees",
    XPathQuery = "//Employee[Age > 30]", // Optional XPath to filter nodes
    IgnoreNamespaces = true,
    AdditionalAttributes = new Dictionary<string, string>
    {
        { "attribute1", "value1" },
        { "attribute2", "value2" }
    }
};

// Use IDataSourceProvider to execute the query
var result = await dataSourceProvider.ExecuteReaderAsync(xmlFileParams);
Console.WriteLine($"\nXML Data Source Result: {JsonSerializer.Serialize(result.Value)}");

