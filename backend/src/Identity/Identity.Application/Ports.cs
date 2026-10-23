using Identity.Domain;

namespace Identity.Application;

// The ports Identity.Application needs. Each is small on purpose (interface segregation); Infrastructure adapts
// SQLite, ASP.NET Core's PasswordHasher and the JWT library to them.

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <param name="normalizedEmail">Already trimmed and lower case (<see cref="User.NormalizeEmail"/>).</param>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <returns><c>false</c> when the email is already registered (the unique index rejected the row).</returns>
    Task<bool> AddAsync(User user, CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}

public interface ITokenService
{
    IssuedToken CreateToken(User user);
}

public sealed record IssuedToken(string Token, DateTime ExpiresAtUtc);
