using Expenses.Application;

namespace Expenses.Infrastructure.Seeding;

/// <summary>
/// Inserts sample expenses for the demo users, spread over the last 90 days and every category, through
/// Expense.Create so they obey the same rules as user input. Idempotent: a user who already has expenses is skipped.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly IExpenseRepository _expenses;
    private readonly TimeProvider _timeProvider;

    public DatabaseSeeder(IExpenseRepository expenses, TimeProvider timeProvider)
    {
        _expenses = expenses;
        _timeProvider = timeProvider;
    }

    /// <returns>How many expenses were inserted.</returns>
    public Task<int> SeedAsync(IEnumerable<SeedUser> users, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
