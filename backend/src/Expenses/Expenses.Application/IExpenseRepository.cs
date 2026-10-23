using Expenses.Domain;

namespace Expenses.Application;

/// <summary>
/// Port for expense persistence. Every read, update and delete is scoped to the owner, so the implementation filters
/// by UserId in SQL and another user's expense is indistinguishable from a missing one.
/// </summary>
public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<ExpensePage> ListAsync(Guid userId, ExpenseListFilter filter, CancellationToken cancellationToken);

    Task AddAsync(Expense expense, CancellationToken cancellationToken);

    /// <returns><c>true</c> if a row owned by <see cref="Expense.UserId"/> was updated.</returns>
    Task<bool> UpdateAsync(Expense expense, CancellationToken cancellationToken);

    /// <returns><c>true</c> if a row owned by <paramref name="userId"/> was deleted.</returns>
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
