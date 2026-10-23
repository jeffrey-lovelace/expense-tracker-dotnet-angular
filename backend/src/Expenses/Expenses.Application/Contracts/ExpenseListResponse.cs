namespace Expenses.Application.Contracts;

// TotalCount and TotalAmount cover every expense matching the filters, not only the current page.
public sealed record ExpenseListResponse(
    IReadOnlyList<ExpenseResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    decimal TotalAmount);
