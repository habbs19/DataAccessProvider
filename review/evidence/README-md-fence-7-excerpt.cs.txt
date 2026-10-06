public class XmlFileSourceParams : BaseDataSourceParams
{
    /// <summary>
    /// Path to the XML file to be read.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// The root element in the XML file where the data begins.
    /// </summary>
    public string RootElement { get; set; }

    /// <summary>
    /// An optional XPath query to select specific nodes from the XML document.
    /// </summary>
    public string? XPathQuery { get; set; }

    /// <summary>
    /// Specifies whether to ignore namespaces in the XML document.
    /// </summary>
    public bool IgnoreNamespaces { get; set; } = true;

    /// <summary>
    /// Additional attributes or settings related to XML parsing.
    /// </summary>
    public Dictionary<string, string>? AdditionalAttributes { get; set; }
}

