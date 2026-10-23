using Expenses.Application.Contracts;
using Expenses.Domain;

namespace Expenses.Application;

// Not implemented yet (TDD red step). The signatures define the contract the tests exercise.
public sealed class ExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpenseService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public Task<Result<ExpenseResponse>> CreateAsync(
        Guid userId, CreateExpenseRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<Result<ExpenseResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<ExpenseResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<Result<ExpenseResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
