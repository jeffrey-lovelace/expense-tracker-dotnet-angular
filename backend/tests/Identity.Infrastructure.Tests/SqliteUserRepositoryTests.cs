using Identity.Domain;
using Identity.Infrastructure.Persistence;

namespace Identity.Infrastructure.Tests;

public sealed class SqliteUserRepositoryTests : IAsyncLifetime
{
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 15, 30, 45, 123, DateTimeKind.Utc);

    private SqliteTestDatabase _database = null!;
    private SqliteUserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _database = await SqliteTestDatabase.CreateAsync();
        _repository = new SqliteUserRepository(_database.ConnectionFactory);
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsEveryFieldUnchanged()
    {
        var user = NewUser("demo@ballastlane.com");

        var added = await _repository.AddAsync(user, CancellationToken.None);
        var loaded = await _repository.GetByIdAsync(user.Id, CancellationToken.None);

        Assert.True(added);
        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded.Id);
        Assert.Equal("demo@ballastlane.com", loaded.Email);
        Assert.Equal("Demo User", loaded.DisplayName);
        Assert.Equal("AQAAAAIAAYagAAAAE-fake-hash", loaded.PasswordHash);
        Assert.Equal(CreatedAt, loaded.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAtUtc.Kind);
    }

    [Fact]
    public async Task GetByEmailAsync_FindsTheUser()
    {
        var user = NewUser("demo@ballastlane.com");
        await _repository.AddAsync(user, CancellationToken.None);

        var loaded = await _repository.GetByEmailAsync("demo@ballastlane.com", CancellationToken.None);

        Assert.Equal(user.Id, loaded!.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_IgnoresCase()
    {
        // The service always normalizes, but the NOCASE column makes the lookup safe even if it did not.
        await _repository.AddAsync(NewUser("demo@ballastlane.com"), CancellationToken.None);

        var loaded = await _repository.GetByEmailAsync("DEMO@BallastLane.com", CancellationToken.None);

        Assert.NotNull(loaded);
    }

    [Fact]
    public async Task GetByEmailAsync_WithUnknownEmail_ReturnsNull()
    {
        Assert.Null(await _repository.GetByEmailAsync("nobody@example.com", CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        Assert.Null(await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Theory]
    [InlineData("demo@ballastlane.com")]
    [InlineData("DEMO@ballastlane.com")]
    public async Task AddAsync_WithTakenEmail_ReturnsFalseAndKeepsOneRow(string secondEmail)
    {
        await _repository.AddAsync(NewUser("demo@ballastlane.com"), CancellationToken.None);

        var added = await _repository.AddAsync(NewUser(secondEmail), CancellationToken.None);

        Assert.False(added);
        Assert.Equal(1L, await _database.ScalarAsync("SELECT COUNT(*) FROM Users"));
    }

    [Fact]
    public async Task AddAsync_WithSqlInDisplayName_StoresItAsPlainText()
    {
        const string hostile = "Robert'); DROP TABLE Users; --";
        var user = User.Rehydrate(Guid.NewGuid(), "bobby@example.com", hostile, "hash", CreatedAt);

        await _repository.AddAsync(user, CancellationToken.None);

        Assert.Equal(hostile, (await _repository.GetByIdAsync(user.Id, CancellationToken.None))!.DisplayName);
    }

    private static User NewUser(string email) =>
        User.Rehydrate(Guid.NewGuid(), email, "Demo User", "AQAAAAIAAYagAAAAE-fake-hash", CreatedAt);
}
