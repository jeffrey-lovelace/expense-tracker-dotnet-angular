namespace Expenses.Domain;

public sealed class Expense
{
    public const int DescriptionMinLength = 3;
    public const int DescriptionMaxLength = 200;
    public const decimal MaxAmount = 1_000_000m;
    public static readonly DateOnly MinDate = new(2000, 1, 1);

    private static readonly string[] CategoryNames = Enum.GetNames<ExpenseCategory>();

    private Expense(
        Guid id,
        Guid userId,
        string description,
        decimal amount,
        ExpenseCategory category,
        DateOnly date,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
    {
        Id = id;
        UserId = userId;
        Description = description;
        Amount = amount;
        Category = category;
        Date = date;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; }

    public Guid UserId { get; }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }

    public ExpenseCategory Category { get; private set; }

    public DateOnly Date { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new expense, enforcing the business rules. <paramref name="now"/> carries the caller's local
    /// offset, so "today" for the not-in-the-future rule is the user's calendar day, not the UTC one.
    /// </summary>
    public static Result<Expense> Create(
        Guid userId,
        string description,
        decimal amount,
        string category,
        DateOnly date,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("The owner id is required.", nameof(userId));
        }

        var fields = Validate(description, amount, category, date, now);
        if (fields.Errors.Count > 0)
        {
            return Result.Failure<Expense>(fields.Errors);
        }

        var nowUtc = now.UtcDateTime;
        return Result.Success(
            new Expense(Guid.NewGuid(), userId, fields.Description, amount, fields.Category, date, nowUtc, nowUtc));
    }

    /// <summary>
    /// Applies an update with the same rules as <see cref="Create"/>. On failure nothing changes.
    /// The owner and the creation time are never changed.
    /// </summary>
    public Result Update(string description, decimal amount, string category, DateOnly date, DateTimeOffset now)
    {
        var fields = Validate(description, amount, category, date, now);
        if (fields.Errors.Count > 0)
        {
            return Result.Failure(fields.Errors);
        }

        Description = fields.Description;
        Amount = amount;
        Category = fields.Category;
        Date = date;
        UpdatedAtUtc = now.UtcDateTime;
        return Result.Success();
    }

    /// <summary>
    /// Rebuilds an expense already stored in the database. No validation: the row was valid when written and the
    /// table CHECKs guard it. Only the repository and tests should call this.
    /// </summary>
    public static Expense Rehydrate(
        Guid id,
        Guid userId,
        string description,
        decimal amount,
        ExpenseCategory category,
        DateOnly date,
        DateTime createdAtUtc,
        DateTime updatedAtUtc) =>
        new(id, userId, description, amount, category, date, createdAtUtc, updatedAtUtc);

    /// <summary>Checks every field and collects all errors, so the form can show them at once.</summary>
    private static ValidatedFields Validate(
        string description, decimal amount, string category, DateOnly date, DateTimeOffset now)
    {
        var errors = new List<Error>();

        var trimmed = description?.Trim() ?? string.Empty;
        if (trimmed.Length is < DescriptionMinLength or > DescriptionMaxLength)
        {
            errors.Add(ExpenseErrors.DescriptionLength);
        }

        if (amount <= 0)
        {
            errors.Add(ExpenseErrors.AmountNotPositive);
        }
        else if (amount > MaxAmount)
        {
            errors.Add(ExpenseErrors.AmountTooLarge);
        }
        else if (decimal.Round(amount, 2) != amount)
        {
            errors.Add(ExpenseErrors.AmountTooManyDecimals);
        }

        // Exact names only. Enum.TryParse would also accept "5", " Food" or "Food,Transport".
        var parsedCategory = ExpenseCategory.Other;
        if (category is not null && CategoryNames.Contains(category, StringComparer.Ordinal))
        {
            parsedCategory = Enum.Parse<ExpenseCategory>(category);
        }
        else
        {
            errors.Add(ExpenseErrors.CategoryUnknown);
        }

        // now.DateTime is the wall clock at the caller's offset, so this is the caller's calendar day.
        var today = DateOnly.FromDateTime(now.DateTime);
        if (date > today)
        {
            errors.Add(ExpenseErrors.DateInFuture);
        }
        else if (date < MinDate)
        {
            errors.Add(ExpenseErrors.DateTooOld);
        }

        return new ValidatedFields(trimmed, parsedCategory, errors);
    }

    private readonly record struct ValidatedFields(string Description, ExpenseCategory Category, List<Error> Errors);
}
