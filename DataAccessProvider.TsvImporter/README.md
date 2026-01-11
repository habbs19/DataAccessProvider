# DataAccessProvider.TsvImporter

A console application for importing IMDb TSV datasets into various databases using the DataAccessProvider library.

## Features

- Reads TSV files (including gzip-compressed `.tsv.gz` files)
- Supports multiple database types:
  - Microsoft SQL Server (MSSQL)
  - MySQL
  - PostgreSQL
- Batch processing for efficient imports
- Automatic schema creation
- Configurable batch sizes and limits
- Progress tracking during import

## IMDb Dataset Format

This importer is designed to work with the IMDb `name.basics.tsv.gz` dataset, which contains:

- `nconst` (string) - Alphanumeric unique identifier of the person
- `primaryName` (string) - Name by which the person is most often credited
- `birthYear` - Birth year in YYYY format
- `deathYear` - Death year in YYYY format (or '\N' if not applicable)
- `primaryProfession` - Array of top-3 professions (comma-separated)
- `knownForTitles` - Array of title IDs the person is known for (comma-separated)

## Configuration

Edit `appsettings.json` to configure:

### Connection Strings

```json
{
  "ConnectionStrings": {
    "MSSQL": "Server=localhost;Database=ImdbData;Trusted_Connection=Yes;TrustServerCertificate=Yes",
    "Postgres": "Host=localhost;Port=5432;Database=imdbdata;Username=postgres;Password=password",
    "MySql": "Server=localhost;Port=3306;Database=imdbdata;Uid=root;Pwd=password;"
  }
}
```

### Import Settings

```json
{
  "ImportSettings": {
    "DatabaseType": "MSSQL",
    "TsvFilePath": "name.basics.tsv.gz",
    "BatchSize": 1000,
    "MaxRecords": 0
  }
}
```

- `DatabaseType`: Target database (MSSQL, MySQL, or Postgres)
- `TsvFilePath`: Path to the TSV file
- `BatchSize`: Number of records to insert per batch
- `MaxRecords`: Maximum records to import (0 = unlimited)

## Usage

### Command Line

```bash
dotnet run
```

Or with arguments:

```bash
dotnet run [tsvFilePath] [databaseType] [batchSize]
```

Examples:

```bash
# Import using settings from appsettings.json
dotnet run

# Import specific file to MySQL
dotnet run name.basics.tsv.gz MySQL 500

# Import to PostgreSQL with larger batch size
dotnet run data/name.basics.tsv Postgres 2000
```

### Database Schema

The importer automatically creates the following table structure:

```sql
CREATE TABLE ImdbPersons (
    Id INT/SERIAL PRIMARY KEY,
    Nconst VARCHAR(20) NOT NULL UNIQUE,
    PrimaryName VARCHAR(500) NOT NULL,
    BirthYear INT NULL,
    DeathYear INT NULL,
    PrimaryProfession VARCHAR(1000) NULL,
    KnownForTitles VARCHAR(1000) NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

Indexes are created on `Nconst` and `PrimaryName` for improved query performance.

## Getting IMDb Data

1. Visit [IMDb Datasets](https://datasets.imdbws.com/)
2. Download `name.basics.tsv.gz`
3. Place it in the project directory or specify the path

## Performance Tips

- Increase `BatchSize` for faster imports (try 5000-10000 for large datasets)
- Use `MaxRecords` during testing to limit import size
- Ensure your database has adequate resources (memory, disk I/O)
- For very large datasets, consider running the import during off-peak hours

## Error Handling

- Invalid/malformed TSV lines are skipped automatically
- Duplicate `Nconst` values will update existing records (upsert behavior)
- Database connection errors will halt the import with an error message

## Dependencies

- DataAccessProvider.Core
- DataAccessProvider.MSSQL
- DataAccessProvider.MySql
- DataAccessProvider.Postgres
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging

## Example Output

```
=== IMDb TSV Data Importer ===

Configuration:
  Database Type: MSSQL
  TSV File: name.basics.tsv.gz
  Batch Size: 1000
  Max Records: Unlimited

Checking database schema...
Schema already exists.

Starting import...
Progress: 1000 records imported...
Progress: 2000 records imported...
Progress: 3000 records imported...

=== Import Complete ===
Total records imported: 3456
Time taken: 45.23 seconds
Records per second: 76.42
```
