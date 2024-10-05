namespace DataAccessProviderConsole.Classes;

public class Movie
{
    /// <summary>
    /// Gets or sets the unique identifier for the movie.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the movie.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description or synopsis of the movie.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the genre of the movie.
    /// </summary>
    public string Genre { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the release date of the movie.
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the director of the movie.
    /// </summary>
    public string Director { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of main actors in the movie.
    /// </summary>
    public List<string> Actors { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the duration of the movie in minutes.
    /// </summary>
    public int Duration { get; set; } // Duration in minutes

    /// <summary>
    /// Gets or sets the rating of the movie (e.g., IMDb rating).
    /// </summary>
    public double Rating { get; set; }

    /// <summary>
    /// Gets or sets the box office revenue of the movie.
    /// </summary>
    public decimal BoxOfficeRevenue { get; set; }

    /// <summary>
    /// Gets or sets the country where the movie was produced.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of languages available for the movie.
    /// </summary>
    public List<string> Languages { get; set; } = new List<string>();
}