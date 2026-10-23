namespace Identity.Infrastructure.Seeding;

/// <summary>
/// The "Seed" configuration section. Users are listed in appsettings.json with fixed ids that Expenses.Api uses too;
/// <see cref="Enabled"/> is true only in appsettings.Development.json (or with Seed__Enabled=true).
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }

    public IList<SeedUser> Users { get; } = [];
}

/// <summary>A demo account. The password is the public demo credential from the README, not a secret.</summary>
public sealed class SeedUser
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
