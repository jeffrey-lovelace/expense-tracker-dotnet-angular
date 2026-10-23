using Expenses.Domain;

namespace Expenses.Application;

/// <summary>
/// Port for expense persistence. Every read and delete takes the owner's id, so the implementation filters by
/// UserId in SQL and another user's expense is indistinguishable from a missing one.
/// </summary>
public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Expense>> GetAllAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(Expense expense, CancellationToken cancellationToken);

    Task UpdateAsync(Expense expense, CancellationToken cancellationToken);

    /// <returns><c>true</c> if a row owned by <paramref name="userId"/> was deleted.</returns>
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
