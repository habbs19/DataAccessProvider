using System.IO.Compression;
using DataAccessProvider.TsvImporter.Models;

namespace DataAccessProvider.TsvImporter.Services;

/// <summary>
/// Service for reading and parsing TSV files, including gzip-compressed files
/// </summary>
public class TsvReader
{
    /// <summary>
    /// Reads IMDb person data from a TSV file (supports .tsv and .tsv.gz files)
    /// </summary>
    /// <param name="filePath">Path to the TSV file</param>
    /// <param name="batchSize">Number of records to yield per batch (0 for all records)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async enumerable of ImdbPerson records</returns>
    public static async IAsyncEnumerable<ImdbPerson> ReadPersonsAsync(
        string filePath,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"TSV file not found: {filePath}");
        }

        Stream fileStream = File.OpenRead(filePath);

        try
        {
            // If the file is gzip-compressed, decompress it
            if (filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                fileStream = new GZipStream(fileStream, CompressionMode.Decompress);
            }

            using var reader = new StreamReader(fileStream);

            // Read and skip the header line
            var header = await reader.ReadLineAsync(cancellationToken);
            if (header == null)
            {
                yield break;
            }

            int count = 0;
            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var person = ParsePersonLine(line);
                if (person != null)
                {
                    yield return person;
                    count++;
                }
            }
        }
        finally
        {
            await fileStream.DisposeAsync();
        }
    }

    /// <summary>
    /// Parses a single TSV line into an ImdbPerson object
    /// </summary>
    private static ImdbPerson? ParsePersonLine(string line)
    {
        try
        {
            var fields = line.Split('\t');

            // Expected format: nconst, primaryName, birthYear, deathYear, primaryProfession, knownForTitles
            if (fields.Length < 6)
            {
                return null;
            }

            var person = new ImdbPerson
            {
                Nconst = fields[0],
                PrimaryName = fields[1],
                BirthYear = ParseYear(fields[2]),
                DeathYear = ParseYear(fields[3]),
                PrimaryProfession = ParseCommaSeparatedField(fields[4]),
                KnownForTitles = ParseCommaSeparatedField(fields[5])
            };

            return person;
        }
        catch
        {
            // Skip malformed lines
            return null;
        }
    }

    /// <summary>
    /// Parses a year field, returning null for '\N' or invalid values
    /// </summary>
    private static int? ParseYear(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "\\N")
        {
            return null;
        }

        if (int.TryParse(value, out int year))
        {
            return year;
        }

        return null;
    }

    /// <summary>
    /// Parses a comma-separated field into a list of strings
    /// </summary>
    private static List<string> ParseCommaSeparatedField(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "\\N")
        {
            return new List<string>();
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(s => s.Trim())
                   .ToList();
    }
}
