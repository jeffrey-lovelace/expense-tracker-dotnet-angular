using Expenses.Application;
using Expenses.Domain;

namespace Expenses.Infrastructure.Persistence;

/// <summary>Adapter from ADO.NET to <see cref="IExpenseRepository"/>. The only class that knows the Expenses SQL.</summary>
public sealed class SqliteExpenseRepository : IExpenseRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteExpenseRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<Expense?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<ExpensePage> ListAsync(Guid userId, ExpenseListFilter filter, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task AddAsync(Expense expense, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<bool> UpdateAsync(Expense expense, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
