using Expenses.Infrastructure.Persistence;

namespace Expenses.Infrastructure.Tests;

public sealed class SqliteMigratorTests
{
    [Fact]
    public async Task MigrateAsync_OnEmptyDatabase_AppliesEveryScriptInOrder()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(migrate: false);
        var migrator = new SqliteMigrator(database.ConnectionFactory);

        var applied = await migrator.MigrateAsync(CancellationToken.None);

        Assert.Equal(["0001_create_expenses"], applied);
        Assert.Equal(1L, await database.ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE name = 'Expenses'"));
        Assert.Equal(1L, await database.ScalarAsync("SELECT Version FROM SchemaVersions WHERE Name = '0001_create_expenses'"));
    }

    [Fact]
    public async Task MigrateAsync_RunTwice_AppliesNothingTheSecondTime()
    {
        await using var database = await SqliteTestDatabase.CreateAsync(migrate: false);
        var migrator = new SqliteMigrator(database.ConnectionFactory);
        await migrator.MigrateAsync(CancellationToken.None);

        var applied = await migrator.MigrateAsync(CancellationToken.None);

        Assert.Empty(applied);
        Assert.Equal(1L, await database.ScalarAsync("SELECT COUNT(*) FROM SchemaVersions"));
    }
}
