using System.Globalization;

namespace Expenses.Domain.Tests;

public sealed class ExpenseTests
{
    // 22:00 on Oct 22 in Bolivia (UTC-4) is already 02:00 on Oct 23 in UTC. "Today" for this caller is the 22nd.
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 22, 0, 0, TimeSpan.FromHours(-4));
    private static readonly DateOnly Today = new(2026, 10, 22);
    private static readonly Guid OwnerId = Guid.Parse("6f1c3a52-8d4e-4b7a-9c21-0e5f7d8a9b10");

    // ---------- Create: valid values ----------

    [Fact]
    public void Create_WithValidValues_ReturnsExpense()
    {
        var result = Expense.Create(OwnerId, "Lunch at the market", 12.50m, "Food", Today, Now);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        var expense = result.Value!;
        Assert.NotEqual(Guid.Empty, expense.Id);
        Assert.Equal(OwnerId, expense.UserId);
        Assert.Equal("Lunch at the market", expense.Description);
        Assert.Equal(12.50m, expense.Amount);
        Assert.Equal(ExpenseCategory.Food, expense.Category);
        Assert.Equal(Today, expense.Date);
        Assert.Equal(Now.UtcDateTime, expense.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, expense.CreatedAtUtc.Kind);
        Assert.Equal(expense.CreatedAtUtc, expense.UpdatedAtUtc);
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDistinctIds()
    {
        var first = Expense.Create(OwnerId, "Coffee", 3m, "Food", Today, Now);
        var second = Expense.Create(OwnerId, "Coffee", 3m, "Food", Today, Now);

        Assert.NotEqual(first.Value!.Id, second.Value!.Id);
    }

    [Fact]
    public void Create_WithEmptyOwner_Throws()
    {
        // A missing owner is a programming error (the id comes from the JWT), not user input.
        Assert.Throws<ArgumentException>(() => Expense.Create(Guid.Empty, "Coffee", 3m, "Food", Today, Now));
    }

    // ---------- Description ----------

    [Fact]
    public void Create_WithSurroundingSpaces_TrimsDescription()
    {
        var result = Expense.Create(OwnerId, "   Bus fare  ", 3m, "Transport", Today, Now);

        Assert.Equal("Bus fare", result.Value!.Description);
    }

    public static TheoryData<string> InvalidDescriptions => new()
    {
        null!,
        string.Empty,
        "     ",
        "ab",
        "  ab  ",
        new string('x', Expense.DescriptionMaxLength + 1),
    };

    [Theory]
    [MemberData(nameof(InvalidDescriptions))]
    public void Create_WithInvalidDescription_ReturnsLengthError(string description)
    {
        var result = Expense.Create(OwnerId, description, 10m, "Food", Today, Now);

        AssertSingleError(result, "description", "expense.description.length");
    }

    public static TheoryData<string> DescriptionsAtBoundary => new()
    {
        "abc",
        "  abc  ",
        new string('x', Expense.DescriptionMaxLength),
    };

    [Theory]
    [MemberData(nameof(DescriptionsAtBoundary))]
    public void Create_WithDescriptionAtBoundary_Succeeds(string description)
    {
        var result = Expense.Create(OwnerId, description, 10m, "Food", Today, Now);

        Assert.True(result.IsSuccess);
    }

    // ---------- Amount ----------

    [Theory]
    [InlineData("0", "expense.amount.not_positive")]
    [InlineData("-0.01", "expense.amount.not_positive")]
    [InlineData("-25", "expense.amount.not_positive")]
    [InlineData("1000000.01", "expense.amount.too_large")]
    [InlineData("12.345", "expense.amount.too_many_decimals")]
    [InlineData("0.001", "expense.amount.too_many_decimals")]
    public void Create_WithInvalidAmount_ReturnsAmountError(string amount, string expectedCode)
    {
        var result = Expense.Create(OwnerId, "Groceries", ParseAmount(amount), "Food", Today, Now);

        AssertSingleError(result, "amount", expectedCode);
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("1000000")]
    [InlineData("12.5")]
    [InlineData("12.500")] // trailing zeros are still two decimal places of value
    public void Create_WithValidAmount_Succeeds(string amount)
    {
        var result = Expense.Create(OwnerId, "Groceries", ParseAmount(amount), "Food", Today, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParseAmount(amount), result.Value!.Amount);
    }

    // ---------- Category ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Groceries")]
    [InlineData("food")]           // exact names only; the UI sends values from GET /api/categories
    [InlineData(" Food")]
    [InlineData("5")]              // Enum.TryParse would accept a number
    [InlineData("Food,Transport")] // Enum.TryParse would combine flags
    public void Create_WithUnknownCategory_ReturnsCategoryError(string? category)
    {
        var result = Expense.Create(OwnerId, "Groceries", 10m, category!, Today, Now);

        AssertSingleError(result, "category", "expense.category.unknown");
    }

    [Fact]
    public void Create_WithEveryKnownCategory_Succeeds()
    {
        foreach (var category in Enum.GetValues<ExpenseCategory>())
        {
            var result = Expense.Create(OwnerId, "Groceries", 10m, category.ToString(), Today, Now);

            Assert.True(result.IsSuccess, category.ToString());
            Assert.Equal(category, result.Value!.Category);
        }
    }

    // ---------- Date ----------

    [Fact]
    public void Create_WithTomorrowInCallersTimeZone_ReturnsInFutureError()
    {
        var result = Expense.Create(OwnerId, "Groceries", 10m, "Food", Today.AddDays(1), Now);

        AssertSingleError(result, "date", "expense.date.in_future");
    }

    [Fact]
    public void Create_WithSameInstantSeenInUtc_AcceptsTheUtcDate()
    {
        // The same instant as Now, but with a UTC offset: there it is already the 23rd.
        var result = Expense.Create(OwnerId, "Groceries", 10m, "Food", Today.AddDays(1), Now.ToUniversalTime());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithToday_Succeeds()
    {
        var result = Expense.Create(OwnerId, "Groceries", 10m, "Food", Today, Now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_WithDateBefore2000_ReturnsTooOldError()
    {
        var result = Expense.Create(OwnerId, "Groceries", 10m, "Food", new DateOnly(1999, 12, 31), Now);

        AssertSingleError(result, "date", "expense.date.too_old");
    }

    [Fact]
    public void Create_WithFirstDayOf2000_Succeeds()
    {
        var result = Expense.Create(OwnerId, "Groceries", 10m, "Food", Expense.MinDate, Now);

        Assert.True(result.IsSuccess);
    }

    // ---------- Several errors ----------

    [Fact]
    public void Create_WithSeveralInvalidFields_ReturnsEveryError()
    {
        string[] expectedFields = ["amount", "category", "date", "description"];

        var result = Expense.Create(OwnerId, " ", 0m, "Groceries", Today.AddDays(1), Now);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.All(result.Errors, e => Assert.Equal(ErrorType.Validation, e.Type));
        Assert.Equal(expectedFields, result.Errors.Select(e => e.Field).Order());
    }

    // ---------- Update ----------

    [Fact]
    public void Update_WithValidValues_ChangesFieldsAndUpdatedAt()
    {
        var expense = Expense.Create(OwnerId, "Coffee", 3m, "Food", Today.AddDays(-3), Now.AddDays(-3)).Value!;
        var later = Now.AddHours(1);

        var result = expense.Update("  Dinner  ", 40m, "Entertainment", Today, later);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dinner", expense.Description);
        Assert.Equal(40m, expense.Amount);
        Assert.Equal(ExpenseCategory.Entertainment, expense.Category);
        Assert.Equal(Today, expense.Date);
        Assert.Equal(later.UtcDateTime, expense.UpdatedAtUtc);
        Assert.Equal(Now.AddDays(-3).UtcDateTime, expense.CreatedAtUtc);
        Assert.Equal(OwnerId, expense.UserId);
    }

    [Fact]
    public void Update_WithInvalidValues_ReturnsEveryErrorAndLeavesExpenseUnchanged()
    {
        var expense = Expense.Create(OwnerId, "Coffee", 3m, "Food", Today, Now).Value!;
        string[] expectedFields = ["amount", "category", "date", "description"];

        var result = expense.Update("x", -1m, "Nope", Today.AddDays(1), Now.AddHours(1));

        Assert.False(result.IsSuccess);
        Assert.Equal(expectedFields, result.Errors.Select(e => e.Field).Order());
        Assert.Equal("Coffee", expense.Description);
        Assert.Equal(3m, expense.Amount);
        Assert.Equal(ExpenseCategory.Food, expense.Category);
        Assert.Equal(Today, expense.Date);
        Assert.Equal(expense.CreatedAtUtc, expense.UpdatedAtUtc);
    }

    // ---------- Helpers ----------

    // InlineData cannot hold decimals, so amounts arrive as invariant strings.
    private static decimal ParseAmount(string amount) => decimal.Parse(amount, CultureInfo.InvariantCulture);

    private static void AssertSingleError(Result<Expense> result, string field, string code)
    {
        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(field, error.Field);
        Assert.Equal(code, error.Code);
    }
}
