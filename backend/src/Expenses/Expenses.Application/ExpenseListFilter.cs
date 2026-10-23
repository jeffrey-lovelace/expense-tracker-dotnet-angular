using Expenses.Domain;

namespace Expenses.Application;

/// <summary>A validated list query, as the repository receives it.</summary>
public sealed record ExpenseListFilter(
    int Page,
    int PageSize,
    ExpenseCategory? Category = null,
    DateOnly? From = null,
    DateOnly? To = null);

/// <summary>One page of expenses plus the totals for every row matching the filter.</summary>
public sealed record ExpensePage(IReadOnlyList<Expense> Items, int TotalCount, decimal TotalAmount);
