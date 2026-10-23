namespace Expenses.Domain;

// Codes are part of the API contract (ProblemDetails); messages are shown to the user as they are.
public static class ExpenseErrors
{
    public static readonly Error DescriptionLength = Error.Validation(
        "description",
        "expense.description.length",
        $"Description must be between {Expense.DescriptionMinLength} and {Expense.DescriptionMaxLength} characters.");

    public static readonly Error AmountNotPositive = Error.Validation(
        "amount", "expense.amount.not_positive", "Amount must be greater than 0.");

    public static readonly Error AmountTooLarge = Error.Validation(
        "amount", "expense.amount.too_large", "Amount must be at most 1,000,000.00.");

    public static readonly Error AmountTooManyDecimals = Error.Validation(
        "amount", "expense.amount.too_many_decimals", "Amount must have at most 2 decimal places.");

    public static readonly Error CategoryUnknown = Error.Validation(
        "category",
        "expense.category.unknown",
        $"Category must be one of: {string.Join(", ", Enum.GetNames<ExpenseCategory>())}.");

    public static readonly Error DateInFuture = Error.Validation(
        "date", "expense.date.in_future", "Date cannot be in the future.");

    public static readonly Error DateTooOld = Error.Validation(
        "date", "expense.date.too_old", "Date cannot be before 2000-01-01.");

    public static readonly Error NotFound = Error.NotFound("expense.not_found", "Expense not found.");
}
