using Identity.Application;
using Identity.Domain;

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
    public async Task<int> SeedAsync(IEnumerable<SeedUser> users, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(users);
        var inserted = 0;

        foreach (var seed in users)
        {
            var email = User.NormalizeEmail(seed.Email);
            if (await _users.GetByIdAsync(seed.Id, cancellationToken) is not null
                || await _users.GetByEmailAsync(email, cancellationToken) is not null)
            {
                continue;
            }

            var created = User.Create(seed.Email, seed.DisplayName, seed.Password, _passwordHasher.Hash, _timeProvider.GetUtcNow());
            if (!created.IsSuccess)
            {
                // Error codes only: the message must not echo the configured password.
                var codes = string.Join(", ", created.Errors.Select(e => e.Code));
                throw new InvalidOperationException($"Seed user '{email}' is invalid: {codes}.");
            }

            // Same validated values, but the fixed id that Expenses.Api also uses for the sample expenses.
            var user = created.Value!;
            var withFixedId = User.Rehydrate(seed.Id, user.Email, user.DisplayName, user.PasswordHash, user.CreatedAtUtc);
            if (await _users.AddAsync(withFixedId, cancellationToken))
            {
                inserted++;
            }
        }

        return inserted;
    }
}
