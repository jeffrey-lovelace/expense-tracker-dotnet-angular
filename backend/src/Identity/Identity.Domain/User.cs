namespace Identity.Domain;

public sealed class User
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMinLength = 2;
    public const int DisplayNameMaxLength = 80;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    private User(Guid id, string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; }

    /// <summary>Trimmed and lower case. Unique, case-insensitive.</summary>
    public string Email { get; }

    public string DisplayName { get; }

    /// <summary>The PBKDF2 hash. The plain password is never stored on the entity.</summary>
    public string PasswordHash { get; }

    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Registers a new user. Every field, including the plain password, is validated first and all errors are
    /// returned together; <paramref name="hashPassword"/> is only called when everything is valid.
    /// </summary>
    public static Result<User> Create(
        string email, string displayName, string password, Func<string, string> hashPassword, DateTimeOffset now) =>
        throw new NotImplementedException();

    /// <summary>The form an email is stored and compared in: trimmed, lower case.</summary>
    public static string NormalizeEmail(string? email) => throw new NotImplementedException();

    /// <summary>Rebuilds a user already stored in the database. No validation.</summary>
    public static User Rehydrate(Guid id, string email, string displayName, string passwordHash, DateTime createdAtUtc) =>
        new(id, email, displayName, passwordHash, createdAtUtc);
}
