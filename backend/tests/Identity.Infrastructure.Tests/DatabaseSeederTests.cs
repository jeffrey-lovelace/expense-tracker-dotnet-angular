using Identity.Infrastructure.Persistence;
using Identity.Infrastructure.Security;
using Identity.Infrastructure.Seeding;

namespace Identity.Infrastructure.Tests;

public sealed class DatabaseSeederTests : IAsyncLifetime
{
    private static readonly Guid DemoId = Guid.Parse("5d1c0b6e-8f2a-4c3e-9b7d-1a2b3c4d5e6f");
    private static readonly Guid OtherId = Guid.Parse("a8f3e2d1-6b4c-4f7a-8e9d-0c1b2a3f4e5d");

    private readonly AspNetPasswordHasher _hasher = new();
    private SqliteTestDatabase _database = null!;
    private SqliteUserRepository _users = null!;
    private DatabaseSeeder _seeder = null!;

    public async Task InitializeAsync()
    {
        _database = await SqliteTestDatabase.CreateAsync();
        _users = new SqliteUserRepository(_database.ConnectionFactory);
        _seeder = new DatabaseSeeder(_users, _hasher, TimeProvider.System);
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task SeedAsync_CreatesUsersWithFixedIdsAndWorkingPasswords()
    {
        var inserted = await _seeder.SeedAsync(DemoUsers(), CancellationToken.None);

        Assert.Equal(2, inserted);
        var demo = await _users.GetByIdAsync(DemoId, CancellationToken.None);
        Assert.NotNull(demo);
        Assert.Equal("demo@ballastlane.com", demo.Email);
        Assert.Equal("Demo User", demo.DisplayName);
        Assert.True(_hasher.Verify(demo.PasswordHash, "Demo123!"));
        Assert.NotEqual("Demo123!", demo.PasswordHash);
        Assert.Equal("other@ballastlane.com", (await _users.GetByIdAsync(OtherId, CancellationToken.None))!.Email);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_InsertsNothingTheSecondTime()
    {
        await _seeder.SeedAsync(DemoUsers(), CancellationToken.None);

        var inserted = await _seeder.SeedAsync(DemoUsers(), CancellationToken.None);

        Assert.Equal(0, inserted);
        Assert.Equal(2L, await _database.ScalarAsync("SELECT COUNT(*) FROM Users"));
    }

    [Fact]
    public async Task SeedAsync_WithInvalidSeedUser_FailsWithAClearMessage()
    {
        var weak = new SeedUser { Id = Guid.NewGuid(), Email = "weak@example.com", DisplayName = "Weak", Password = "weak" };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _seeder.SeedAsync([weak], CancellationToken.None));

        Assert.Contains("weak@example.com", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("weak\"", exception.Message, StringComparison.Ordinal);
    }

    private static SeedUser[] DemoUsers() =>
    [
        new() { Id = DemoId, Email = "demo@ballastlane.com", DisplayName = "Demo User", Password = "Demo123!" },
        new() { Id = OtherId, Email = "other@ballastlane.com", DisplayName = "Second User", Password = "Demo123!" },
    ];
}
