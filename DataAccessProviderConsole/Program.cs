using DataAccessProvider.Core.DataSource;
using DataAccessProvider.Core.DataSource.Params;
using DataAccessProvider.Core.DataSource.Source;
using DataAccessProvider.Core.Interfaces;
using DataAccessProvider.MSSQL;
using DataAccessProvider.MySql;
using DataAccessProviderConsole.Classes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System.ComponentModel;
using System.Text.Json;

var serviceProvider = ConfigureServices();

// Resolve the IDataSourceProvider and use it

var dataSourceProvider1 = serviceProvider.GetService<IDataSourceProvider>();
var dataSourceProvider2 = serviceProvider.GetService<IDataSourceProvider<StaticCodeParams>>();

var jsonFileParams1 = new JsonFileSourceParams
{
    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "Movie.json"),
};
var codeParams1 = new StaticCodeParams
{
    Content = "Hello World"
};
var mssqParams1 = new MSSQLSourceParams
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
mssqParams1.CommandType = System.Data.CommandType.Text;
var mssqParams3 = new MSSQLSourceParams<Diary>
{
    Query = "SELECT TOP 1 * FROM [HS].[dbo].[Diary]"
};
mssqParams3.CommandType = System.Data.CommandType.Text;

var result1a = await dataSourceProvider1!.ExecuteReaderAsync(jsonFileParams1);
var result1b = await dataSourceProvider1!.ExecuteReaderAsync(codeParams1);
var result1c = await dataSourceProvider1!.ExecuteReaderAsync(mssqParams1);
var result1d = await dataSourceProvider1!.ExecuteReaderAsync(mssqParams3);
//var result1d = await dataSourceProvider2!.ExecuteScalarAsync(codeParams1);
Console.WriteLine($"\n1a:  {JsonSerializer.Serialize(result1a.Value)}");
Console.WriteLine($"\n1b:  {JsonSerializer.Serialize(result1b.Value)}");
Console.WriteLine($"\n1b:  {JsonSerializer.Serialize(result1c.Value)}");
Console.WriteLine($"\n1b:  {JsonSerializer.Serialize(result1d.Value)}");

//Console.WriteLine($"\n1c:  {JsonSerializer.Serialize(result1c.Value)}");
//Console.WriteLine($"\n1d:  {JsonSerializer.Serialize(result1d.Value)}");


/// test with type return
var codeParams2 = new StaticCodeParams<string>
{
    Content = "Hello World"
};
var mssqParams2 = new MSSQLSourceParams<Diary>
{
    Query = "SELECT TOP 3 * FROM [HS].[dbo].[Diary]"
};
mssqParams2.CommandType = System.Data.CommandType.Text;

var jsonFileParams2 = new JsonFileSourceParams<Movie>
{
    FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "Movie.json"),
};

var jsonFileParams3 = new JsonFileSourceParams<List<Genre>>
{
    FilePath = Path.Combine("C:\\Users\\habibs\\source\\repos\\aznV\\aznV.Infrastructure\\TestData", "Genres.json"),
};
var jsonFileParams3Result = await dataSourceProvider1!.ExecuteReaderAsync(jsonFileParams3);
Console.WriteLine($"\n3:  {JsonSerializer.Serialize(jsonFileParams3Result.Value)}");

var myParams = new MySQLSourceParams
{
    Query = "SP_RegistrationCRUD"
};
var json1 = new { Email = "hs_19@hotmail.com", OTPCode = 9839081 };
myParams.Parameters!.AddParameter("Operation", MySql.Data.MySqlClient.MySqlDbType.UInt16, 3);
myParams.Parameters!.AddParameter("Params", MySqlDbType.JSON, JsonSerializer.Serialize(json1));
var myParamsResult = await dataSourceProvider1!.ExecuteReaderAsync(myParams);
Console.WriteLine($"\n4:  {JsonSerializer.Serialize(myParamsResult.Value)}");

var appuserParams = new MySQLSourceParams<AppUser>
{
    Query = "SP_UserEmailStore"
};
appuserParams.Parameters.AddParameter("Operation", MySqlDbType.UInt16, 1);
var json2 = new { Email = "hs_19@hotmail.com" };
appuserParams.Parameters.AddParameter("Params", MySqlDbType.JSON, JsonSerializer.Serialize(json2));

var appuserParamsResult = await dataSourceProvider1!.ExecuteReaderAsync(appuserParams);
Console.WriteLine($"\n5:  {JsonSerializer.Serialize(appuserParamsResult.Value?.FirstOrDefault())}");






static ServiceProvider ConfigureServices()
{
    string sqlString = "Server=HABIB;Database=HS;Trusted_Connection=Yes;TrustServerCertificate=Yes";
    string postgresString = "";
    string mySqlString = "Server=127.0.0.1;Port=3310;Database=aznv;Uid=root;Pwd=password;";

    var services = new ServiceCollection();

    // Register necessary services
    services.AddSingleton<IDataSourceProvider, DataSourceProvider>();
    services.AddSingleton(typeof(IDataSourceProvider<>), typeof(DataSourceProvider<>));
    services.AddSingleton<IDataSourceFactory, DataSourceFactory>();

    // Add database source services

    services.AddScoped<IDataSource<MSSQLSourceParams>, MSSQLSource>(provider => new MSSQLSource(sqlString));
    //services.AddScoped<IDataSource<PostgresSourceParams>, PostgresSource>(provider => new PostgresSource(postgresString));
    services.AddScoped<IDataSource<MySQLSourceParams>, MySQLSource>((factory) => new MySQLSource(mySqlString));
    
    //services.AddScoped(factory => new PostgresSource(postgresString));
    services.AddScoped(factory => new MySQLSource(mySqlString));
    services.AddScoped(factory => new MSSQLSource(sqlString));
    services.AddScoped<JsonFileSource>();
    services.AddScoped<StaticCodeSource>();

    //services.AddScoped<IDataSource, PostgresSource>();
    //services.AddScoped<IDataSource, OracleDataSource>();
    //services.AddScoped<IDataSource, MongoDBSource>();
    //services.AddScoped<IDataSource, StaticCodeSource>();


    return services.BuildServiceProvider();
}

class Diary
{
    public int DiaryID { get; set; }
    public string UserID { get; set; }
    public string Category { get; set; }
    public DateTime Date { get; set; }
    public string Content { get; set; }
}

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

public class AppUser : IdentityUser<int>
{
    public AppUser() { }

    public AppUser(string userName) : base(userName) { }

    public int MemberId { get => Id; set => Id = value; }
    public MemberGroupEnum Group { get; set; } = MemberGroupEnum.regular;
    //public string? Group { get; set; } = "regular";
    public string? InviteCode { get; set; }
    public int DateJoined { get; set; }
    public int? LastVisit { get; set; }
    public int? LastTopup { get; set; }
    public int? LastInviteSent { get; set; }
}
public enum MemberGroupEnum
{
    regular,
    premium,
    vip,
    mod,
    Admin
}
/// <summary>
/// Represents a user in the identity system
/// </summary>
/// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
public class IdentityUser<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Initializes a new instance of <see cref="IdentityUser{TKey}"/>.
    /// </summary>
    public IdentityUser() { }

    /// <summary>
    /// Initializes a new instance of <see cref="IdentityUser{TKey}"/>.
    /// </summary>
    /// <param name="userName">The user name.</param>
    public IdentityUser(string userName) : this()
    {
        Username = userName;
    }

    public virtual TKey Id { get; set; } = default!;


    public virtual string? Username { get; set; }
    public virtual string? NormalizedUsername { get; set; }

 
    public virtual string? Email { get; set; }
    public virtual string? NormalizedEmail { get; set; }
    public virtual bool EmailConfirmed { get; set; }
    public virtual string? PasswordHash { get; set; }
    public virtual string? SecurityStamp { get; set; }
    public virtual string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
    public virtual string? PhoneNumber { get; set; }
    public virtual bool PhoneNumberConfirmed { get; set; }

    public virtual bool TwoFactorEnabled { get; set; }
    public virtual DateTimeOffset? LockoutEnd { get; set; }
    public virtual bool LockoutEnabled { get; set; }
    public virtual int AccessFailedCount { get; set; }

    public override string ToString()
        => Username ?? string.Empty;
}
