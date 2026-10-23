using Expenses.Application;
using Expenses.Domain;

namespace Expenses.Infrastructure.Seeding;

/// <summary>
/// Inserts sample expenses for the demo users, spread over the last 90 days and every category, through
/// Expense.Create so they obey the same rules as user input. Idempotent: a user who already has expenses is skipped.
/// </summary>
public sealed class DatabaseSeeder
{
    private const int DaysBack = 90;

    // One template per category, in enum order, so the first nine expenses cover every category.
    private static readonly SampleExpense[] Samples =
    [
        new("Groceries at the market", ExpenseCategory.Food, 15m, 120m),
        new("Taxi to the office", ExpenseCategory.Transport, 8m, 45m),
        new("Monthly rent", ExpenseCategory.Housing, 900m, 1400m),
        new("Electricity bill", ExpenseCategory.Utilities, 40m, 110m),
        new("Pharmacy", ExpenseCategory.Health, 10m, 80m),
        new("Cinema tickets", ExpenseCategory.Entertainment, 12m, 40m),
        new("New running shoes", ExpenseCategory.Shopping, 30m, 150m),
        new("Online course", ExpenseCategory.Education, 20m, 200m),
        new("Gift for a friend", ExpenseCategory.Other, 15m, 90m),
        new("Lunch with the team", ExpenseCategory.Food, 12m, 60m),
        new("Bus card top-up", ExpenseCategory.Transport, 5m, 25m),
        new("Internet plan", ExpenseCategory.Utilities, 30m, 60m),
        new("Coffee and pastry", ExpenseCategory.Food, 4m, 12m),
        new("Streaming subscription", ExpenseCategory.Entertainment, 8m, 18m),
        new("Books", ExpenseCategory.Education, 15m, 70m),
    ];

    private readonly IExpenseRepository _expenses;
    private readonly TimeProvider _timeProvider;

    public DatabaseSeeder(IExpenseRepository expenses, TimeProvider timeProvider)
    {
        _expenses = expenses;
        _timeProvider = timeProvider;
    }

    /// <returns>How many expenses were inserted.</returns>
    public async Task<int> SeedAsync(IEnumerable<SeedUser> users, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(users);
        var now = _timeProvider.GetLocalNow();
        var today = DateOnly.FromDateTime(now.DateTime);
        var inserted = 0;

        foreach (var user in users)
        {
            var existing = await _expenses.ListAsync(user.Id, new ExpenseListFilter(1, 1), cancellationToken);
            if (existing.TotalCount > 0)
            {
                continue;
            }

            // Seeded from the user id: the same user always gets the same sample data.
            var random = new Random(BitConverter.ToInt32(user.Id.ToByteArray(), 0));
            for (var i = 0; i < user.ExpenseCount; i++)
            {
                var sample = Samples[i % Samples.Length];
                var amount = decimal.Round(sample.Min + ((sample.Max - sample.Min) * (decimal)random.NextDouble()), 2);
                var date = today.AddDays(-random.Next(DaysBack));

                var created = Expense.Create(user.Id, sample.Description, amount, sample.Category.ToString(), date, now);
                if (!created.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Sample expense '{sample.Description}' is invalid: {string.Join(", ", created.Errors.Select(e => e.Code))}.");
                }

                await _expenses.AddAsync(created.Value!, cancellationToken);
                inserted++;
            }
        }

        return inserted;
    }

    private sealed record SampleExpense(string Description, ExpenseCategory Category, decimal Min, decimal Max);
}
