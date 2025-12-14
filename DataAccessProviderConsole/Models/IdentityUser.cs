namespace DataAccessProviderConsole.Models;

/// <summary>
/// Represents a user in the identity system.
/// </summary>
/// <typeparam name="TKey">The type used for the primary key for the user.</typeparam>
public class IdentityUser<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Initializes a new instance of <see cref="IdentityUser{TKey}"/>.
    /// </summary>
    public IdentityUser()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IdentityUser{TKey}"/>.
    /// </summary>
    /// <param name="userName">The user name.</param>
    public IdentityUser(string userName)
        : this()
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