using Expenses.Application.Contracts;
using Expenses.Domain;

namespace Expenses.Application.Mapping;

public static class ExpenseMappings
{
    public static ExpenseResponse ToResponse(this Expense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);
        return new ExpenseResponse(
            expense.Id,
            expense.Description,
            expense.Amount,
            expense.Category.ToString(),
            expense.Date,
            expense.CreatedAtUtc,
            expense.UpdatedAtUtc);
    }
}
