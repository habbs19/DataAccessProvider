namespace DataAccessProviderConsole.Models;

/// <summary>
/// Represents a genre category and its items.
/// </summary>
public class Genre
{
    /// <summary>
    /// Gets or sets the category of the genre (e.g., anime, movies, drama).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of items (genres) under the category.
    /// </summary>
    public List<string> Items { get; set; } = new List<string>();
}