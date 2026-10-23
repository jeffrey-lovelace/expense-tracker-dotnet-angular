using Identity.Infrastructure.Persistence;

namespace Identity.Infrastructure.Tests;

public sealed class SqliteMigratorTests
{
    [Fact]
    public async Task MigrateAsync_OnEmptyDatabase_CreatesUsersTable()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(migrate: false);

        var applied = await new SqliteMigrator(database.ConnectionFactory).MigrateAsync(CancellationToken.None);

        Assert.Equal(["0001_create_users"], applied);
        Assert.Equal(1L, await database.ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE name = 'Users'"));
        Assert.Equal(1L, await database.ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE name = 'UX_Users_Email'"));
    }

    [Fact]
    public async Task MigrateAsync_RunTwice_AppliesNothingTheSecondTime()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(migrate: false);
        var migrator = new SqliteMigrator(database.ConnectionFactory);
        await migrator.MigrateAsync(CancellationToken.None);

        Assert.Empty(await migrator.MigrateAsync(CancellationToken.None));
    }
}
