namespace Expenses.Application.Contracts;

// No UserId on purpose: the owner always comes from the JWT, passed by the caller of the service.
// Category is a string so an unknown value becomes a field validation error instead of a JSON binding failure.
public sealed record CreateExpenseRequest(string Description, decimal Amount, string Category, DateOnly Date);
