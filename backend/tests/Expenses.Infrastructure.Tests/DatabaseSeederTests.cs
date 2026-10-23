using Expenses.Application;
using Expenses.Domain;
using Expenses.Infrastructure.Persistence;
using Expenses.Infrastructure.Seeding;

namespace Expenses.Infrastructure.Tests;

public sealed class DatabaseSeederTests : IAsyncLifetime
{
    private static readonly Guid DemoId = Guid.Parse("5d1c0b6e-8f2a-4c3e-9b7d-1a2b3c4d5e6f");
    private static readonly Guid OtherId = Guid.Parse("a8f3e2d1-6b4c-4f7a-8e9d-0c1b2a3f4e5d");

    private SqliteTestDatabase _database = null!;
    private SqliteExpenseRepository _expenses = null!;
    private DatabaseSeeder _seeder = null!;

    public async Task InitializeAsync()
    {
        _database = await SqliteTestDatabase.CreateAsync();
        _expenses = new SqliteExpenseRepository(_database.ConnectionFactory);
        _seeder = new DatabaseSeeder(_expenses, TimeProvider.System);
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task SeedAsync_CreatesTheRequestedNumberOfExpensesPerUser()
    {
        var inserted = await _seeder.SeedAsync(DemoUsers(), CancellationToken.None);

        Assert.Equal(33, inserted);
        Assert.Equal(30, (await ListAll(DemoId)).Count);
        Assert.Equal(3, (await ListAll(OtherId)).Count);
    }

    [Fact]
    public async Task SeedAsync_CoversEveryCategoryForTheDemoUser()
    {
        await _seeder.SeedAsync(DemoUsers(), CancellationToken.None);

        var categories = (await ListAll(DemoId)).Select(e => e.Category).Distinct().Order();

        Assert.Equal(Enum.GetValues<ExpenseCategory>().Order(), categories);
    }

    [Fact]
    public async Task SeedAsync_CreatesOnlyValidRecentExpenses()
    {
        await _seeder.SeedAsync(DemoUsers(), CancellationToken.None);
        var today = DateOnly.FromDateTime(DateTime.Now);

        var expenses = await ListAll(DemoId);

        Assert.All(expenses, e =>
        {
            Assert.Equal(DemoId, e.UserId);
            Assert.InRange(e.Date, today.AddDays(-89), today);
            Assert.InRange(e.Amount, 0.01m, Expense.MaxAmount);
            Assert.Equal(decimal.Round(e.Amount, 2), e.Amount);
            Assert.InRange(e.Description.Length, Expense.DescriptionMinLength, Expense.DescriptionMaxLength);
        });
    }

    [Fact]
    public async Task SeedAsync_RunTwice_InsertsNothingTheSecondTime()
    {
        await _seeder.SeedAsync(DemoUsers(), CancellationToken.None);

        var inserted = await _seeder.SeedAsync(DemoUsers(), CancellationToken.None);

        Assert.Equal(0, inserted);
        Assert.Equal(33L, await _database.ScalarAsync("SELECT COUNT(*) FROM Expenses"));
    }

    private async Task<IReadOnlyList<Expense>> ListAll(Guid userId) =>
        (await _expenses.ListAsync(userId, new ExpenseListFilter(1, 100), CancellationToken.None)).Items;

    private static SeedUser[] DemoUsers() =>
    [
        new() { Id = DemoId, ExpenseCount = 30 },
        new() { Id = OtherId, ExpenseCount = 3 },
    ];
}
