using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using DataAccessProvider.Postgres;
using DataAccessProvider.TsvImporter.Models;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Npgsql;
using System.Data;
using System.Text;

namespace DataAccessProvider.TsvImporter.Services;

/// <summary>
/// Service for importing IMDb data into a database using DataAccessProvider
/// </summary>
public class DatabaseImporter
{
    private readonly IDataSourceProvider _dataSourceProvider;
    private readonly ILogger<DatabaseImporter> _logger;
    private readonly string _databaseType;

    public DatabaseImporter(
        IDataSourceProvider dataSourceProvider,
        ILogger<DatabaseImporter> logger,
        string databaseType)
    {
        _dataSourceProvider = dataSourceProvider;
        _logger = logger;
        _databaseType = databaseType;
    }

    /// <summary>
    /// Creates the database schema for storing IMDb person data
    /// </summary>
    public async Task CreateSchemaAsync()
    {
        _logger.LogInformation("Creating database schema...");

        var createTableQuery = _databaseType.ToLower() switch
        {
            "mssql" => GetMssqlCreateTableQuery(),
            "mysql" => GetMySqlCreateTableQuery(),
            "postgres" => GetPostgresCreateTableQuery(),
            _ => throw new NotSupportedException($"Database type '{_databaseType}' is not supported")
        };

        try
        {
            var result = await ExecuteNonQueryAsync(createTableQuery);
            _logger.LogInformation("Database schema created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create database schema");
            throw;
        }
    }

    /// <summary>
    /// Imports a batch of persons into the database
    /// </summary>
    public async Task<int> ImportBatchAsync(List<ImdbPerson> persons)
    {
        if (!persons.Any())
        {
            return 0;
        }

        _logger.LogInformation("Importing batch of {Count} persons...", persons.Count);

        try
        {
            var insertQuery = _databaseType.ToLower() switch
            {
                "mssql" => GetMssqlInsertQuery(),
                "mysql" => GetMySqlInsertQuery(),
                "postgres" => GetPostgresInsertQuery(),
                _ => throw new NotSupportedException($"Database type '{_databaseType}' is not supported")
            };

            int totalInserted = 0;

            // Insert records one by one (can be optimized with bulk insert later)
            foreach (var person in persons)
            {
                var affectedRows = await ExecuteInsertAsync(insertQuery, person);
                totalInserted += affectedRows;
            }

            _logger.LogInformation("Successfully imported {Count} persons", totalInserted);
            return totalInserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import batch");
            throw;
        }
    }

    /// <summary>
    /// Checks if the table exists in the database
    /// </summary>
    public async Task<bool> TableExistsAsync()
    {
        var checkTableQuery = _databaseType.ToLower() switch
        {
            "mssql" => "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ImdbPersons'",
            "mysql" => "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ImdbPersons' AND TABLE_SCHEMA = DATABASE()",
            "postgres" => "SELECT COUNT(*) FROM information_schema.tables WHERE table_name = 'imdbpersons'",
            _ => throw new NotSupportedException($"Database type '{_databaseType}' is not supported")
        };

        try
        {
            var result = await ExecuteScalarAsync<long>(checkTableQuery);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if table exists");
            return false;
        }
    }

    private async Task<int> ExecuteNonQueryAsync(string query)
    {
        return _databaseType.ToLower() switch
        {
            "mssql" => await ExecuteMssqlNonQueryAsync(query),
            "mysql" => await ExecuteMySqlNonQueryAsync(query),
            "postgres" => await ExecutePostgresNonQueryAsync(query),
            _ => throw new NotSupportedException($"Database type '{_databaseType}' is not supported")
        };
    }

    private async Task<int> ExecuteInsertAsync(string query, ImdbPerson person)
    {
        return _databaseType.ToLower() switch
        {
            "mssql" => await ExecuteMssqlInsertAsync(query, person),
            "mysql" => await ExecuteMySqlInsertAsync(query, person),
            "postgres" => await ExecutePostgresInsertAsync(query, person),
            _ => throw new NotSupportedException($"Database type '{_databaseType}' is not supported")
        };
    }

    private async Task<T> ExecuteScalarAsync<T>(string query)
    {
        return _databaseType.ToLower() switch
        {
            "mssql" => await ExecuteMssqlScalarAsync<T>(query),
            "mysql" => await ExecuteMySqlScalarAsync<T>(query),
            "postgres" => await ExecutePostgresScalarAsync<T>(query),
            _ => throw new NotSupportedException($"Database type '{_databaseType}' is not supported")
        };
    }

    #region MSSQL Methods

    private async Task<int> ExecuteMssqlNonQueryAsync(string query)
    {
        var parameters = new MSSQLSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        var result = await _dataSourceProvider.ExecuteNonQueryAsync(parameters);
        return result.AffectedRows;
    }

    private async Task<int> ExecuteMssqlInsertAsync(string query, ImdbPerson person)
    {
        var parameters = new MSSQLSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        parameters.AddParameter("@Nconst", System.Data.SqlDbType.NVarChar, person.Nconst);
        parameters.AddParameter("@PrimaryName", System.Data.SqlDbType.NVarChar, person.PrimaryName);
        parameters.AddParameter("@BirthYear", System.Data.SqlDbType.Int, person.BirthYear);
        parameters.AddParameter("@DeathYear", System.Data.SqlDbType.Int, person.DeathYear);
        parameters.AddParameter("@PrimaryProfession", System.Data.SqlDbType.NVarChar, person.PrimaryProfessionString);
        parameters.AddParameter("@KnownForTitles", System.Data.SqlDbType.NVarChar, person.KnownForTitlesString);

        var result = await _dataSourceProvider.ExecuteNonQueryAsync(parameters);
        return result.AffectedRows;
    }

    private async Task<T> ExecuteMssqlScalarAsync<T>(string query)
    {
        var parameters = new MSSQLSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        var result = await _dataSourceProvider.ExecuteScalarAsync(parameters);
        return (T)Convert.ChangeType(result.Value!, typeof(T));
    }

    private string GetMssqlCreateTableQuery()
    {
        return @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ImdbPersons')
            BEGIN
                CREATE TABLE ImdbPersons (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Nconst NVARCHAR(20) NOT NULL UNIQUE,
                    PrimaryName NVARCHAR(500) NOT NULL,
                    BirthYear INT NULL,
                    DeathYear INT NULL,
                    PrimaryProfession NVARCHAR(1000) NULL,
                    KnownForTitles NVARCHAR(1000) NULL,
                    CreatedAt DATETIME2 DEFAULT GETDATE()
                );
                CREATE INDEX IX_ImdbPersons_Nconst ON ImdbPersons(Nconst);
                CREATE INDEX IX_ImdbPersons_PrimaryName ON ImdbPersons(PrimaryName);
            END";
    }

    private string GetMssqlInsertQuery()
    {
        return @"
            INSERT INTO ImdbPersons (Nconst, PrimaryName, BirthYear, DeathYear, PrimaryProfession, KnownForTitles)
            VALUES (@Nconst, @PrimaryName, @BirthYear, @DeathYear, @PrimaryProfession, @KnownForTitles)";
    }

    #endregion

    #region MySQL Methods

    private async Task<int> ExecuteMySqlNonQueryAsync(string query)
    {
        var parameters = new MySQLSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        var result = await _dataSourceProvider.ExecuteNonQueryAsync(parameters);
        return result.AffectedRows;
    }

    private async Task<int> ExecuteMySqlInsertAsync(string query, ImdbPerson person)
    {
        var parameters = new MySQLSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        parameters.AddParameter("@Nconst", MySqlDbType.VarChar, person.Nconst);
        parameters.AddParameter("@PrimaryName", MySqlDbType.VarChar, person.PrimaryName);
        parameters.AddParameter("@BirthYear", MySqlDbType.Int32, person.BirthYear);
        parameters.AddParameter("@DeathYear", MySqlDbType.Int32, person.DeathYear);
        parameters.AddParameter("@PrimaryProfession", MySqlDbType.VarChar, person.PrimaryProfessionString);
        parameters.AddParameter("@KnownForTitles", MySqlDbType.VarChar, person.KnownForTitlesString);

        var result = await _dataSourceProvider.ExecuteNonQueryAsync(parameters);
        return result.AffectedRows;
    }

    private async Task<T> ExecuteMySqlScalarAsync<T>(string query)
    {
        var parameters = new MySQLSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        var result = await _dataSourceProvider.ExecuteScalarAsync(parameters);
        return (T)Convert.ChangeType(result.Value!, typeof(T));
    }

    private string GetMySqlCreateTableQuery()
    {
        return @"
            CREATE TABLE IF NOT EXISTS ImdbPersons (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Nconst VARCHAR(20) NOT NULL UNIQUE,
                PrimaryName VARCHAR(500) NOT NULL,
                BirthYear INT NULL,
                DeathYear INT NULL,
                PrimaryProfession VARCHAR(1000) NULL,
                KnownForTitles VARCHAR(1000) NULL,
                CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                INDEX IX_ImdbPersons_Nconst (Nconst),
                INDEX IX_ImdbPersons_PrimaryName (PrimaryName)
            )";
    }

    private string GetMySqlInsertQuery()
    {
        return @"
            INSERT INTO ImdbPersons (Nconst, PrimaryName, BirthYear, DeathYear, PrimaryProfession, KnownForTitles)
            VALUES (@Nconst, @PrimaryName, @BirthYear, @DeathYear, @PrimaryProfession, @KnownForTitles)
            ON DUPLICATE KEY UPDATE
                PrimaryName = VALUES(PrimaryName),
                BirthYear = VALUES(BirthYear),
                DeathYear = VALUES(DeathYear),
                PrimaryProfession = VALUES(PrimaryProfession),
                KnownForTitles = VALUES(KnownForTitles)";
    }

    #endregion

    #region Postgres Methods

    private async Task<int> ExecutePostgresNonQueryAsync(string query)
    {
        var parameters = new PostgresSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        var result = await _dataSourceProvider.ExecuteNonQueryAsync(parameters);
        return result.AffectedRows;
    }

    private async Task<int> ExecutePostgresInsertAsync(string query, ImdbPerson person)
    {
        var parameters = new PostgresSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        parameters.AddParameter("@Nconst", NpgsqlTypes.NpgsqlDbType.Varchar, person.Nconst);
        parameters.AddParameter("@PrimaryName", NpgsqlTypes.NpgsqlDbType.Varchar, person.PrimaryName);
        parameters.AddParameter("@BirthYear", NpgsqlTypes.NpgsqlDbType.Integer, person.BirthYear);
        parameters.AddParameter("@DeathYear", NpgsqlTypes.NpgsqlDbType.Integer, person.DeathYear);
        parameters.AddParameter("@PrimaryProfession", NpgsqlTypes.NpgsqlDbType.Varchar, person.PrimaryProfessionString);
        parameters.AddParameter("@KnownForTitles", NpgsqlTypes.NpgsqlDbType.Varchar, person.KnownForTitlesString);

        var result = await _dataSourceProvider.ExecuteNonQueryAsync(parameters);
        return result.AffectedRows;
    }

    private async Task<T> ExecutePostgresScalarAsync<T>(string query)
    {
        var parameters = new PostgresSourceParams
        {
            Query = query,
            CommandType = CommandType.Text
        };

        var result = await _dataSourceProvider.ExecuteScalarAsync(parameters);
        return (T)Convert.ChangeType(result.Value!, typeof(T));
    }

    private string GetPostgresCreateTableQuery()
    {
        return @"
            CREATE TABLE IF NOT EXISTS ImdbPersons (
                Id SERIAL PRIMARY KEY,
                Nconst VARCHAR(20) NOT NULL UNIQUE,
                PrimaryName VARCHAR(500) NOT NULL,
                BirthYear INT NULL,
                DeathYear INT NULL,
                PrimaryProfession VARCHAR(1000) NULL,
                KnownForTitles VARCHAR(1000) NULL,
                CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            CREATE INDEX IF NOT EXISTS IX_ImdbPersons_Nconst ON ImdbPersons(Nconst);
            CREATE INDEX IF NOT EXISTS IX_ImdbPersons_PrimaryName ON ImdbPersons(PrimaryName);";
    }

    private string GetPostgresInsertQuery()
    {
        return @"
            INSERT INTO ImdbPersons (Nconst, PrimaryName, BirthYear, DeathYear, PrimaryProfession, KnownForTitles)
            VALUES (@Nconst, @PrimaryName, @BirthYear, @DeathYear, @PrimaryProfession, @KnownForTitles)
            ON CONFLICT (Nconst) DO UPDATE SET
                PrimaryName = EXCLUDED.PrimaryName,
                BirthYear = EXCLUDED.BirthYear,
                DeathYear = EXCLUDED.DeathYear,
                PrimaryProfession = EXCLUDED.PrimaryProfession,
                KnownForTitles = EXCLUDED.KnownForTitles";
    }

    #endregion
}
