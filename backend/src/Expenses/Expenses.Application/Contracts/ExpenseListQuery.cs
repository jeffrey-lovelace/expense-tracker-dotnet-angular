namespace Expenses.Application.Contracts;

// Bound from the query string: GET /api/expenses?page=1&pageSize=20&category=Food&from=2026-10-01&to=2026-10-31
public sealed record ExpenseListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Category = null,
    DateOnly? From = null,
    DateOnly? To = null);
