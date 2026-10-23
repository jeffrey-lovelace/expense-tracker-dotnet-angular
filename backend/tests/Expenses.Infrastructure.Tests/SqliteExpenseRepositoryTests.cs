using System.Globalization;

using Expenses.Application;
using Expenses.Domain;
using Expenses.Infrastructure.Persistence;

namespace Expenses.Infrastructure.Tests;

public sealed class SqliteExpenseRepositoryTests : IAsyncLifetime
{
    private static readonly Guid OwnerId = Guid.Parse("6f1c3a52-8d4e-4b7a-9c21-0e5f7d8a9b10");
    private static readonly Guid OtherUserId = Guid.Parse("a2d94e17-3b6c-4f08-8e5a-71c9b0d3f264");
    private static readonly DateTime CreatedAt = new(2026, 10, 1, 15, 30, 45, 123, DateTimeKind.Utc);

    private SqliteTestDatabase _database = null!;
    private SqliteExpenseRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _database = await SqliteTestDatabase.CreateAsync();
        _repository = new SqliteExpenseRepository(_database.ConnectionFactory);
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    // ---------- Add / GetById ----------

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsEveryFieldUnchanged()
    {
        var expense = NewExpense(OwnerId, "Lunch at the market", 12.50m, ExpenseCategory.Food, new DateOnly(2026, 10, 22));

        await _repository.AddAsync(expense, CancellationToken.None);
        var loaded = await _repository.GetByIdAsync(OwnerId, expense.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(expense.Id, loaded.Id);
        Assert.Equal(OwnerId, loaded.UserId);
        Assert.Equal("Lunch at the market", loaded.Description);
        Assert.Equal(12.50m, loaded.Amount);
        Assert.Equal(ExpenseCategory.Food, loaded.Category);
        Assert.Equal(new DateOnly(2026, 10, 22), loaded.Date);
        Assert.Equal(CreatedAt, loaded.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAtUtc.Kind);
        Assert.Equal(CreatedAt, loaded.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("12.50", 1250L)]
    [InlineData("0.01", 1L)]
    [InlineData("0.10", 10L)]
    [InlineData("1000000", 100000000L)]
    public async Task AddAsync_StoresAmountAsIntegerCents(string amount, long expectedCents)
    {
        var expense = NewExpense(OwnerId, amount: decimal.Parse(amount, CultureInfo.InvariantCulture));

        await _repository.AddAsync(expense, CancellationToken.None);

        var id = ("@id", (object)expense.Id.ToString("D"));
        Assert.Equal(expectedCents, await _database.ScalarAsync("SELECT AmountCents FROM Expenses WHERE Id = @id", id));
        Assert.Equal("integer", await _database.ScalarAsync("SELECT typeof(AmountCents) FROM Expenses WHERE Id = @id", id));
    }

    [Fact]
    public async Task AddAsync_StoresDateAsCalendarDateText()
    {
        var expense = NewExpense(OwnerId, date: new DateOnly(2026, 2, 28));

        await _repository.AddAsync(expense, CancellationToken.None);

        Assert.Equal("2026-02-28", await _database.ScalarAsync(
            "SELECT ExpenseDate FROM Expenses WHERE Id = @id", ("@id", expense.Id.ToString("D"))));
    }

    [Fact]
    public async Task AddAsync_WithSqlInDescription_StoresItAsPlainText()
    {
        const string hostile = "O'Reilly'); DROP TABLE Expenses; --";
        var expense = NewExpense(OwnerId, hostile);

        await _repository.AddAsync(expense, CancellationToken.None);
        var loaded = await _repository.GetByIdAsync(OwnerId, expense.Id, CancellationToken.None);

        Assert.Equal(hostile, loaded!.Description);
        Assert.Equal(1L, await _database.ScalarAsync("SELECT COUNT(*) FROM Expenses"));
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherUsersExpense_ReturnsNull()
    {
        var expense = NewExpense(OwnerId);
        await _repository.AddAsync(expense, CancellationToken.None);

        var loaded = await _repository.GetByIdAsync(OtherUserId, expense.Id, CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
    {
        var loaded = await _repository.GetByIdAsync(OwnerId, Guid.NewGuid(), CancellationToken.None);

        Assert.Null(loaded);
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateAsync_WithOwnExpense_SavesChanges()
    {
        var expense = NewExpense(OwnerId, "Coffee", 3m, ExpenseCategory.Food, new DateOnly(2026, 10, 1));
        await _repository.AddAsync(expense, CancellationToken.None);
        var updatedAt = CreatedAt.AddHours(2);
        var changed = Expense.Rehydrate(
            expense.Id, OwnerId, "Dinner", 40.25m, ExpenseCategory.Entertainment, new DateOnly(2026, 10, 2), CreatedAt, updatedAt);

        var saved = await _repository.UpdateAsync(changed, CancellationToken.None);

        Assert.True(saved);
        var loaded = await _repository.GetByIdAsync(OwnerId, expense.Id, CancellationToken.None);
        Assert.Equal("Dinner", loaded!.Description);
        Assert.Equal(40.25m, loaded.Amount);
        Assert.Equal(ExpenseCategory.Entertainment, loaded.Category);
        Assert.Equal(new DateOnly(2026, 10, 2), loaded.Date);
        Assert.Equal(CreatedAt, loaded.CreatedAtUtc);
        Assert.Equal(updatedAt, loaded.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_WithOtherUsersExpense_ChangesNothing()
    {
        var expense = NewExpense(OwnerId, "Coffee");
        await _repository.AddAsync(expense, CancellationToken.None);
        var forged = Expense.Rehydrate(
            expense.Id, OtherUserId, "Hacked", 1m, ExpenseCategory.Other, expense.Date, CreatedAt, CreatedAt.AddHours(1));

        var saved = await _repository.UpdateAsync(forged, CancellationToken.None);

        Assert.False(saved);
        var loaded = await _repository.GetByIdAsync(OwnerId, expense.Id, CancellationToken.None);
        Assert.Equal("Coffee", loaded!.Description);
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteAsync_WithOwnExpense_RemovesIt()
    {
        var expense = NewExpense(OwnerId);
        await _repository.AddAsync(expense, CancellationToken.None);

        var deleted = await _repository.DeleteAsync(OwnerId, expense.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Null(await _repository.GetByIdAsync(OwnerId, expense.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_WithOtherUsersExpense_KeepsIt()
    {
        var expense = NewExpense(OwnerId);
        await _repository.AddAsync(expense, CancellationToken.None);

        var deleted = await _repository.DeleteAsync(OtherUserId, expense.Id, CancellationToken.None);

        Assert.False(deleted);
        Assert.NotNull(await _repository.GetByIdAsync(OwnerId, expense.Id, CancellationToken.None));
    }

    // ---------- List ----------

    [Fact]
    public async Task ListAsync_ReturnsOnlyOwnersExpenses_NewestDateFirst()
    {
        var older = NewExpense(OwnerId, date: new DateOnly(2026, 10, 1));
        var newer = NewExpense(OwnerId, date: new DateOnly(2026, 10, 20));
        var theirs = NewExpense(OtherUserId, date: new DateOnly(2026, 10, 21));
        await AddAllAsync(older, newer, theirs);

        var page = await _repository.ListAsync(OwnerId, new ExpenseListFilter(1, 20), CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], page.Items.Select(e => e.Id));
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task ListAsync_WithSameDate_OrdersByCreationNewestFirst()
    {
        var date = new DateOnly(2026, 10, 5);
        var first = NewExpense(OwnerId, date: date, createdAt: CreatedAt);
        var second = NewExpense(OwnerId, date: date, createdAt: CreatedAt.AddMinutes(5));
        await AddAllAsync(first, second);

        var page = await _repository.ListAsync(OwnerId, new ExpenseListFilter(1, 20), CancellationToken.None);

        Assert.Equal([second.Id, first.Id], page.Items.Select(e => e.Id));
    }

    [Fact]
    public async Task ListAsync_WithPaging_ReturnsRequestedPageAndTotalsForAllRows()
    {
        var expenses = Enumerable.Range(1, 5)
            .Select(day => NewExpense(OwnerId, amount: day, date: new DateOnly(2026, 10, day)))
            .ToArray();
        await AddAllAsync(expenses);

        var page = await _repository.ListAsync(OwnerId, new ExpenseListFilter(2, 2), CancellationToken.None);

        // Newest first: days 5, 4 | 3, 2 | 1. Page 2 holds days 3 and 2.
        Assert.Equal([3m, 2m], page.Items.Select(e => e.Amount));
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(15m, page.TotalAmount);
    }

    [Fact]
    public async Task ListAsync_PastTheLastPage_ReturnsNoItemsButKeepsTotals()
    {
        await AddAllAsync(NewExpense(OwnerId, amount: 7.5m));

        var page = await _repository.ListAsync(OwnerId, new ExpenseListFilter(3, 20), CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(7.5m, page.TotalAmount);
    }

    [Fact]
    public async Task ListAsync_WithCategory_ReturnsOnlyThatCategory()
    {
        var food = NewExpense(OwnerId, category: ExpenseCategory.Food, amount: 10m);
        var bus = NewExpense(OwnerId, category: ExpenseCategory.Transport, amount: 2m);
        await AddAllAsync(food, bus);

        var page = await _repository.ListAsync(
            OwnerId, new ExpenseListFilter(1, 20, ExpenseCategory.Transport), CancellationToken.None);

        Assert.Equal(bus.Id, Assert.Single(page.Items).Id);
        Assert.Equal(2m, page.TotalAmount);
    }

    [Fact]
    public async Task ListAsync_WithDateRange_IncludesBothEnds()
    {
        var before = NewExpense(OwnerId, date: new DateOnly(2026, 9, 30));
        var start = NewExpense(OwnerId, date: new DateOnly(2026, 10, 1));
        var end = NewExpense(OwnerId, date: new DateOnly(2026, 10, 31));
        var after = NewExpense(OwnerId, date: new DateOnly(2026, 11, 1));
        await AddAllAsync(before, start, end, after);

        var filter = new ExpenseListFilter(1, 20, From: new DateOnly(2026, 10, 1), To: new DateOnly(2026, 10, 31));
        var page = await _repository.ListAsync(OwnerId, filter, CancellationToken.None);

        Assert.Equal([end.Id, start.Id], page.Items.Select(e => e.Id));
    }

    [Fact]
    public async Task ListAsync_WithNoExpenses_ReturnsZeroTotals()
    {
        var page = await _repository.ListAsync(OwnerId, new ExpenseListFilter(1, 20), CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0m, page.TotalAmount);
    }

    // ---------- Helpers ----------

    private static Expense NewExpense(
        Guid ownerId,
        string description = "Groceries",
        decimal amount = 10m,
        ExpenseCategory category = ExpenseCategory.Food,
        DateOnly? date = null,
        DateTime? createdAt = null) =>
        Expense.Rehydrate(
            Guid.NewGuid(),
            ownerId,
            description,
            amount,
            category,
            date ?? new DateOnly(2026, 10, 10),
            createdAt ?? CreatedAt,
            createdAt ?? CreatedAt);

    private async Task AddAllAsync(params Expense[] expenses)
    {
        foreach (var expense in expenses)
        {
            await _repository.AddAsync(expense, CancellationToken.None);
        }
    }
}
