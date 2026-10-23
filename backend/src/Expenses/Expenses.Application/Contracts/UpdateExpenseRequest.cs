namespace Expenses.Application.Contracts;

// Same shape as CreateExpenseRequest. The owner cannot be changed by an update.
public sealed record UpdateExpenseRequest(string Description, decimal Amount, string Category, DateOnly Date);
