namespace Expenses.Domain;

public sealed class Expense
{
    public const int DescriptionMinLength = 3;
    public const int DescriptionMaxLength = 200;
    public const decimal MaxAmount = 1_000_000m;
    public static readonly DateOnly MinDate = new(2000, 1, 1);

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
        DateTimeOffset now) =>
        throw new NotImplementedException();

    /// <summary>Applies an update with the same rules as <see cref="Create"/>.</summary>
    public Result Update(string description, decimal amount, string category, DateOnly date, DateTimeOffset now) =>
        throw new NotImplementedException();

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
}
