using System.Text.RegularExpressions;

namespace Identity.Domain;

public sealed partial class User
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
        string email, string displayName, string password, Func<string, string> hashPassword, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(hashPassword);

        var errors = new List<Error>();

        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail.Length > EmailMaxLength || !EmailFormat().IsMatch(normalizedEmail))
        {
            errors.Add(UserErrors.EmailInvalid);
        }

        var trimmedName = displayName?.Trim() ?? string.Empty;
        if (trimmedName.Length is < DisplayNameMinLength or > DisplayNameMaxLength)
        {
            errors.Add(UserErrors.DisplayNameLength);
        }

        // Not trimmed: spaces are characters the user chose.
        AddPasswordErrors(password ?? string.Empty, errors);

        if (errors.Count > 0)
        {
            return Result.Failure<User>(errors);
        }

        return Result.Success(
            new User(Guid.NewGuid(), normalizedEmail, trimmedName, hashPassword(password!), now.UtcDateTime));
    }

    /// <summary>The form an email is stored and compared in: trimmed, lower case.</summary>
    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Rebuilds a user already stored in the database. No validation.</summary>
    public static User Rehydrate(Guid id, string email, string displayName, string passwordHash, DateTime createdAtUtc) =>
        new(id, email, displayName, passwordHash, createdAtUtc);

    private static void AddPasswordErrors(string password, List<Error> errors)
    {
        if (password.Length is < PasswordMinLength or > PasswordMaxLength)
        {
            errors.Add(UserErrors.PasswordLength);
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add(UserErrors.PasswordUppercase);
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add(UserErrors.PasswordLowercase);
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add(UserErrors.PasswordDigit);
        }

        if (password.All(char.IsLetterOrDigit))
        {
            errors.Add(UserErrors.PasswordSymbol);
        }
    }

    // Deliberately simple: one @, no spaces, a dot in the domain. Full RFC 5322 validation is not worth it;
    // a typo is caught by the user, not by a regex.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailFormat();
}
