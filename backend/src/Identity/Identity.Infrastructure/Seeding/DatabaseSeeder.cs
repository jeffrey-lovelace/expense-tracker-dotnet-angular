using Identity.Application;

namespace Identity.Infrastructure.Seeding;

/// <summary>
/// Inserts the demo users through the same rules as registration (User.Create, PBKDF2) but with their fixed ids.
/// Idempotent: a user whose id or email already exists is skipped.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public DatabaseSeeder(IUserRepository users, IPasswordHasher passwordHasher, TimeProvider timeProvider)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
    }

    /// <returns>How many users were inserted.</returns>
    public Task<int> SeedAsync(IEnumerable<SeedUser> users, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
