using Expenses.Application.Contracts;
using Expenses.Domain;

using Moq;

namespace Expenses.Application.Tests;

// Field rules (amount, description, category, date) are tested once, in Expenses.Domain.Tests.
// These tests cover what the service adds: ownership, not-found, the clock it passes, what it saves, and the list
// query rules that belong to the Application layer (paging limits, From <= To, category filter).
public sealed class ExpenseServiceTests
{
    // 2026-10-23 02:00 UTC is still 2026-10-22 22:00 in Bolivia (UTC-4). "Today" for the user is the 22nd.
    private static readonly DateTimeOffset UtcNow = new(2026, 10, 23, 2, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly LocalToday = new(2026, 10, 22);

    private static readonly Guid CallerId = Guid.Parse("6f1c3a52-8d4e-4b7a-9c21-0e5f7d8a9b10");
    private static readonly Guid OtherUserId = Guid.Parse("a2d94e17-3b6c-4f08-8e5a-71c9b0d3f264");

    private readonly Mock<IExpenseRepository> _repository = new();
    private readonly ExpenseService _service;

    public ExpenseServiceTests()
    {
        _service = new ExpenseService(_repository.Object, new FixedTimeProvider(UtcNow, FixedTimeProvider.Bolivia));
    }

    // ---------- Create ----------

    [Fact]
    public async Task CreateAsync_WithValidRequest_SavesExpenseOwnedByCaller()
    {
        var request = new CreateExpenseRequest("Lunch at the market", 12.50m, "Food", LocalToday);
        Expense? saved = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()))
            .Callback<Expense, CancellationToken>((expense, _) => saved = expense)
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(saved);
        Assert.Equal(CallerId, saved.UserId);
        Assert.Equal(saved.Id, result.Value!.Id);
        Assert.Equal("Lunch at the market", result.Value.Description);
        Assert.Equal(12.50m, result.Value.Amount);
        Assert.Equal("Food", result.Value.Category);
        Assert.Equal(LocalToday, result.Value.Date);
        Assert.Equal(UtcNow.UtcDateTime, result.Value.CreatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_WhenDomainRejectsRequest_ReturnsItsErrorsAndDoesNotSave()
    {
        var request = new CreateExpenseRequest("Groceries", 0m, "Food", LocalToday);

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("amount", Assert.Single(result.Errors).Field);
        AssertNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_PassesUsersLocalClock_SoTomorrowInBoliviaIsRejected()
    {
        // In UTC it is already the 23rd. If the service passed the UTC clock, this date would be accepted.
        var request = new CreateExpenseRequest("Groceries", 10m, "Food", LocalToday.AddDays(1));

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("date", Assert.Single(result.Errors).Field);
        AssertNothingSaved();
    }

    // ---------- GetById ----------

    [Fact]
    public async Task GetByIdAsync_WithOwnExpense_ReturnsIt()
    {
        var expense = ExistingExpense(CallerId);
        SetupGetById(CallerId, expense);

        var result = await _service.GetByIdAsync(CallerId, expense.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(expense.Id, result.Value!.Id);
        Assert.Equal(expense.Description, result.Value.Description);
    }

    [Fact]
    public async Task GetByIdAsync_WhenExpenseDoesNotExist_ReturnsNotFound()
    {
        var result = await _service.GetByIdAsync(CallerId, Guid.NewGuid(), CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherUsersExpense_ReturnsNotFound()
    {
        var expense = ExistingExpense(OtherUserId);
        SetupGetById(OtherUserId, expense);

        var result = await _service.GetByIdAsync(CallerId, expense.Id, CancellationToken.None);

        AssertNotFound(result);
        _repository.Verify(r => r.GetByIdAsync(CallerId, expense.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryReturnsOtherUsersExpense_ReturnsNotFound()
    {
        // Defense in depth: even if the repository filter were broken, the service never leaks another user's data.
        var expense = ExistingExpense(OtherUserId);
        SetupGetById(CallerId, expense);

        var result = await _service.GetByIdAsync(CallerId, expense.Id, CancellationToken.None);

        AssertNotFound(result);
    }

    // ---------- List ----------

    [Fact]
    public async Task ListAsync_WithDefaultQuery_AsksRepositoryForCallersFirstPage()
    {
        var mine = new[] { ExistingExpense(CallerId), ExistingExpense(CallerId) };
        ExpenseListFilter? received = null;
        _repository
            .Setup(r => r.ListAsync(CallerId, It.IsAny<ExpenseListFilter>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, ExpenseListFilter, CancellationToken>((_, filter, _) => received = filter)
            .ReturnsAsync(new ExpensePage(mine, 42, 1234.56m));
        using var cts = new CancellationTokenSource();

        var result = await _service.ListAsync(CallerId, new ExpenseListQuery(), cts.Token);

        Assert.True(result.IsSuccess);
        Assert.Equal(new ExpenseListFilter(1, 20), received);
        var page = result.Value!;
        Assert.Equal(mine.Select(e => e.Id), page.Items.Select(e => e.Id));
        Assert.Equal(1, page.Page);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(42, page.TotalCount);
        Assert.Equal(1234.56m, page.TotalAmount);
        _repository.Verify(r => r.ListAsync(CallerId, It.IsAny<ExpenseListFilter>(), cts.Token), Times.Once);
        _repository.Verify(r => r.ListAsync(OtherUserId, It.IsAny<ExpenseListFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_WithFilters_PassesParsedFilterToRepository()
    {
        var from = new DateOnly(2026, 10, 1);
        var to = new DateOnly(2026, 10, 31);
        _repository
            .Setup(r => r.ListAsync(CallerId, It.IsAny<ExpenseListFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExpensePage([], 0, 0m));

        var result = await _service.ListAsync(CallerId, new ExpenseListQuery(3, 50, "Food", from, to), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Verify(
            r => r.ListAsync(CallerId, new ExpenseListFilter(3, 50, ExpenseCategory.Food, from, to), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(-1, 20, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, 101, "pageSize")]
    public async Task ListAsync_WithInvalidPaging_ReturnsValidationError(int pageNumber, int pageSize, string field)
    {
        var result = await _service.ListAsync(CallerId, new ExpenseListQuery(pageNumber, pageSize), CancellationToken.None);

        AssertListValidationError(result, field);
    }

    [Fact]
    public async Task ListAsync_WithUnknownCategory_ReturnsValidationError()
    {
        var result = await _service.ListAsync(CallerId, new ExpenseListQuery(Category: "food"), CancellationToken.None);

        AssertListValidationError(result, "category");
    }

    [Fact]
    public async Task ListAsync_WithFromAfterTo_ReturnsValidationError()
    {
        var query = new ExpenseListQuery(From: new DateOnly(2026, 10, 2), To: new DateOnly(2026, 10, 1));

        var result = await _service.ListAsync(CallerId, query, CancellationToken.None);

        AssertListValidationError(result, "to");
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateAsync_WithOwnExpense_SavesChanges()
    {
        var expense = ExistingExpense(CallerId);
        SetupGetById(CallerId, expense);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = new UpdateExpenseRequest("Dinner", 40m, "Entertainment", LocalToday);

        var result = await _service.UpdateAsync(CallerId, expense.Id, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dinner", result.Value!.Description);
        Assert.Equal(40m, result.Value.Amount);
        Assert.Equal("Entertainment", result.Value.Category);
        Assert.Equal(UtcNow.UtcDateTime, result.Value.UpdatedAtUtc);
        _repository.Verify(
            r => r.UpdateAsync(It.Is<Expense>(e => e.Id == expense.Id && e.UserId == CallerId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenDomainRejectsRequest_ReturnsItsErrorsAndDoesNotSave()
    {
        var expense = ExistingExpense(CallerId);
        SetupGetById(CallerId, expense);
        var request = new UpdateExpenseRequest("Dinner", 0m, "Food", LocalToday);

        var result = await _service.UpdateAsync(CallerId, expense.Id, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("amount", Assert.Single(result.Errors).Field);
        AssertNothingUpdated();
    }

    [Fact]
    public async Task UpdateAsync_WhenRowDisappearsBeforeSave_ReturnsNotFound()
    {
        // Deleted by another request between the read and the write: the UPDATE affects 0 rows.
        var expense = ExistingExpense(CallerId);
        SetupGetById(CallerId, expense);
        _repository.Setup(r => r.UpdateAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var request = new UpdateExpenseRequest("Dinner", 40m, "Food", LocalToday);

        var result = await _service.UpdateAsync(CallerId, expense.Id, request, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task UpdateAsync_WhenExpenseDoesNotExist_ReturnsNotFound()
    {
        var request = new UpdateExpenseRequest("Dinner", 40m, "Food", LocalToday);

        var result = await _service.UpdateAsync(CallerId, Guid.NewGuid(), request, CancellationToken.None);

        AssertNotFound(result);
        AssertNothingUpdated();
    }

    [Fact]
    public async Task UpdateAsync_WithOtherUsersExpense_ReturnsNotFound()
    {
        var expense = ExistingExpense(OtherUserId);
        SetupGetById(OtherUserId, expense);
        var request = new UpdateExpenseRequest("Dinner", 40m, "Food", LocalToday);

        var result = await _service.UpdateAsync(CallerId, expense.Id, request, CancellationToken.None);

        AssertNotFound(result);
        AssertNothingUpdated();
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteAsync_WithOwnExpense_Succeeds()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(CallerId, id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.DeleteAsync(CallerId, id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_WhenExpenseDoesNotExist_ReturnsNotFound()
    {
        var result = await _service.DeleteAsync(CallerId, Guid.NewGuid(), CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task DeleteAsync_WithOtherUsersExpense_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(OtherUserId, id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _service.DeleteAsync(CallerId, id, CancellationToken.None);

        AssertNotFound(result);
        _repository.Verify(r => r.DeleteAsync(OtherUserId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Helpers ----------

    private static Expense ExistingExpense(Guid ownerId) =>
        Expense.Rehydrate(
            Guid.NewGuid(),
            ownerId,
            "Coffee beans",
            18.90m,
            ExpenseCategory.Food,
            new DateOnly(2026, 10, 1),
            new DateTime(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 15, 0, 0, DateTimeKind.Utc));

    private void SetupGetById(Guid userId, Expense expense) =>
        _repository.Setup(r => r.GetByIdAsync(userId, expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

    private void AssertNothingSaved() =>
        _repository.Verify(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()), Times.Never);

    private void AssertNothingUpdated() =>
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()), Times.Never);

    private void AssertListValidationError(Result<ExpenseListResponse> result, string field)
    {
        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(field, error.Field);
        _repository.Verify(
            r => r.ListAsync(It.IsAny<Guid>(), It.IsAny<ExpenseListFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void AssertNotFound<T>(Result<T> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, Assert.Single(result.Errors).Type);
    }

    private static void AssertNotFound(Result result)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, Assert.Single(result.Errors).Type);
    }
}
