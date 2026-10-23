using System.Globalization;

using Expenses.Application.Contracts;
using Expenses.Domain;

using Moq;

namespace Expenses.Application.Tests;

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
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.Equal(saved.Id, result.Value!.Id);
        Assert.Equal("Lunch at the market", result.Value.Description);
        Assert.Equal(12.50m, result.Value.Amount);
        Assert.Equal("Food", result.Value.Category);
        Assert.Equal(LocalToday, result.Value.Date);
        Assert.Equal(UtcNow.UtcDateTime, result.Value.CreatedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_WithSurroundingSpaces_TrimsDescription()
    {
        var request = new CreateExpenseRequest("   Bus fare  ", 3m, "Transport", LocalToday);

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Bus fare", result.Value!.Description);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    [InlineData("-25")]
    [InlineData("1000000.01")]
    [InlineData("12.345")]
    public async Task CreateAsync_WithInvalidAmount_ReturnsAmountValidationError(string amount)
    {
        var request = new CreateExpenseRequest("Groceries", ParseAmount(amount), "Food", LocalToday);

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        AssertValidationError(result, "amount");
        AssertNothingSaved();
    }

    [Theory]
    [InlineData("1000000")]
    [InlineData("0.01")]
    public async Task CreateAsync_WithAmountAtBoundary_Succeeds(string amount)
    {
        var request = new CreateExpenseRequest("Groceries", ParseAmount(amount), "Food", LocalToday);

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    public static TheoryData<string> InvalidDescriptions => new()
    {
        string.Empty,
        "     ",
        "ab",
        "  ab  ",
        new string('x', Expense.DescriptionMaxLength + 1),
    };

    [Theory]
    [MemberData(nameof(InvalidDescriptions))]
    public async Task CreateAsync_WithInvalidDescription_ReturnsDescriptionValidationError(string description)
    {
        var request = new CreateExpenseRequest(description, 10m, "Food", LocalToday);

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        AssertValidationError(result, "description");
        AssertNothingSaved();
    }

    [Theory]
    [InlineData("Groceries")]
    [InlineData("")]
    [InlineData("5")]
    public async Task CreateAsync_WithUnknownCategory_ReturnsCategoryValidationError(string category)
    {
        var request = new CreateExpenseRequest("Groceries", 10m, category, LocalToday);

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        AssertValidationError(result, "category");
        AssertNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_WithTomorrowInUserTimeZone_ReturnsDateValidationError()
    {
        // In UTC it is already the 23rd, but for a user in Bolivia the 23rd is still tomorrow.
        var request = new CreateExpenseRequest("Groceries", 10m, "Food", LocalToday.AddDays(1));

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        AssertValidationError(result, "date");
        AssertNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_WithDateBefore2000_ReturnsDateValidationError()
    {
        var request = new CreateExpenseRequest("Groceries", 10m, "Food", new DateOnly(1999, 12, 31));

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        AssertValidationError(result, "date");
        AssertNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_WithSeveralInvalidFields_ReturnsEveryError()
    {
        var request = new CreateExpenseRequest(" ", 0m, "Groceries", LocalToday.AddDays(1));
        string[] expectedFields = ["amount", "category", "date", "description"];

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.All(result.Errors, e => Assert.Equal(ErrorType.Validation, e.Type));
        Assert.Equal(expectedFields, result.Errors.Select(e => e.Field).Distinct().Order());
        AssertNothingSaved();
    }

    [Fact]
    public async Task CreateAsync_WithTodayInUserTimeZone_Succeeds()
    {
        var request = new CreateExpenseRequest("Groceries", 10m, "Food", LocalToday);

        var result = await _service.CreateAsync(CallerId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
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

    // ---------- GetAll ----------

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyCallersExpenses()
    {
        var mine = new[] { ExistingExpense(CallerId), ExistingExpense(CallerId) };
        var theirs = new[] { ExistingExpense(OtherUserId) };
        _repository.Setup(r => r.GetAllAsync(CallerId, It.IsAny<CancellationToken>())).ReturnsAsync(mine);
        _repository.Setup(r => r.GetAllAsync(OtherUserId, It.IsAny<CancellationToken>())).ReturnsAsync(theirs);
        using var cts = new CancellationTokenSource();

        var result = await _service.GetAllAsync(CallerId, cts.Token);

        Assert.Equal(mine.Select(e => e.Id), result.Select(e => e.Id));
        _repository.Verify(r => r.GetAllAsync(CallerId, cts.Token), Times.Once);
        _repository.Verify(r => r.GetAllAsync(OtherUserId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenRepositoryReturnsOtherUsersExpense_ExcludesIt()
    {
        var mine = ExistingExpense(CallerId);
        var leaked = ExistingExpense(OtherUserId);
        _repository
            .Setup(r => r.GetAllAsync(CallerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mine, leaked });

        var result = await _service.GetAllAsync(CallerId, CancellationToken.None);

        Assert.Equal(mine.Id, Assert.Single(result).Id);
    }

    // ---------- Update ----------

    [Fact]
    public async Task UpdateAsync_WithOwnExpense_SavesChanges()
    {
        var expense = ExistingExpense(CallerId);
        SetupGetById(CallerId, expense);
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
    public async Task UpdateAsync_WithInvalidAmount_ReturnsValidationErrorAndDoesNotSave()
    {
        var expense = ExistingExpense(CallerId);
        SetupGetById(CallerId, expense);
        var request = new UpdateExpenseRequest("Dinner", 0m, "Food", LocalToday);

        var result = await _service.UpdateAsync(CallerId, expense.Id, request, CancellationToken.None);

        AssertValidationError(result, "amount");
        AssertNothingUpdated();
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

    // InlineData cannot hold decimals, so amounts arrive as invariant strings.
    private static decimal ParseAmount(string amount) => decimal.Parse(amount, CultureInfo.InvariantCulture);

    private void SetupGetById(Guid userId, Expense expense) =>
        _repository.Setup(r => r.GetByIdAsync(userId, expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

    private void AssertNothingSaved() =>
        _repository.Verify(r => r.AddAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()), Times.Never);

    private void AssertNothingUpdated() =>
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Expense>(), It.IsAny<CancellationToken>()), Times.Never);

    // Only the given field is reported, so a test cannot pass because some other rule happened to fail.
    private static void AssertValidationError<T>(Result<T> result, string field)
    {
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
        Assert.All(result.Errors, e =>
        {
            Assert.Equal(ErrorType.Validation, e.Type);
            Assert.Equal(field, e.Field);
        });
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
