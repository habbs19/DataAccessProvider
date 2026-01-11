namespace DataAccessProvider.TsvImporter.Models;

/// <summary>
/// Represents a person from the IMDb name.basics.tsv dataset
/// </summary>
public class ImdbPerson
{
    /// <summary>
    /// Alphanumeric unique identifier of the name/person (e.g., nm0000001)
    /// </summary>
    public string Nconst { get; set; } = string.Empty;

    /// <summary>
    /// Name by which the person is most often credited
    /// </summary>
    public string PrimaryName { get; set; } = string.Empty;

    /// <summary>
    /// Birth year in YYYY format, null if not available
    /// </summary>
    public int? BirthYear { get; set; }

    /// <summary>
    /// Death year in YYYY format, null if not applicable or not available
    /// </summary>
    public int? DeathYear { get; set; }

    /// <summary>
    /// The top-3 professions of the person (comma-separated in TSV)
    /// </summary>
    public List<string> PrimaryProfession { get; set; } = new();

    /// <summary>
    /// Titles the person is known for (comma-separated tconsts in TSV)
    /// </summary>
    public List<string> KnownForTitles { get; set; } = new();

    /// <summary>
    /// Gets primary profession as comma-separated string for database storage
    /// </summary>
    public string PrimaryProfessionString => string.Join(",", PrimaryProfession);

    /// <summary>
    /// Gets known for titles as comma-separated string for database storage
    /// </summary>
    public string KnownForTitlesString => string.Join(",", KnownForTitles);

    public override string ToString()
    {
        return $"{Nconst}: {PrimaryName} ({BirthYear}{(DeathYear.HasValue ? $"-{DeathYear}" : "")})";
    }
}
