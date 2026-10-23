using Expenses.Domain;

namespace Expenses.Application;

// List query rules belong to the Application layer (AGENTS.md 10.10): they are about reading, not about an expense.
public static class ExpenseQueryErrors
{
    public const int MaxPageSize = 100;

    public static readonly Error Page = Error.Validation(
        "page", "expense.query.page", "Page must be 1 or greater.");

    public static readonly Error PageSize = Error.Validation(
        "pageSize", "expense.query.page_size", $"Page size must be between 1 and {MaxPageSize}.");

    public static readonly Error DateRange = Error.Validation(
        "to", "expense.query.date_range", "The end date must be on or after the start date.");
}
