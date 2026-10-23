namespace Expenses.Application.Contracts;

// UserId is not returned: the caller already knows it is their own expense.
public sealed record ExpenseResponse(
    Guid Id,
    string Description,
    decimal Amount,
    string Category,
    DateOnly Date,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
