namespace Expenses.Infrastructure.Seeding;

/// <summary>
/// The "Seed" configuration section. The user ids are the same fixed ids Identity.Api seeds, so the demo login sees
/// its sample expenses. <see cref="Enabled"/> is true only in appsettings.Development.json (or Seed__Enabled=true).
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }

    public IList<SeedUser> Users { get; } = [];
}

public sealed class SeedUser
{
    public Guid Id { get; set; }

    public int ExpenseCount { get; set; }
}
