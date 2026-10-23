using Expenses.Application.Contracts;
using Expenses.Application.Mapping;
using Expenses.Domain;

namespace Expenses.Application;

/// <summary>
/// Expense use cases. The caller passes the user id taken from the JWT; every operation is scoped to it.
/// Business rules live in <see cref="Expense"/>; this class orchestrates and enforces ownership.
/// </summary>
public sealed class ExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpenseService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ExpenseResponse>> CreateAsync(
        Guid userId, CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // GetLocalNow keeps the local offset, so "today" is the user's calendar day (see DESIGN.md).
        var created = Expense.Create(
            userId, request.Description, request.Amount, request.Category, request.Date, _timeProvider.GetLocalNow());
        if (!created.IsSuccess)
        {
            return Result.Failure<ExpenseResponse>(created.Errors);
        }

        var expense = created.Value!;
        await _repository.AddAsync(expense, cancellationToken);
        return Result.Success(expense.ToResponse());
    }

    public async Task<Result<ExpenseResponse>> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var expense = await FindOwnedAsync(userId, id, cancellationToken);
        return expense is null
            ? Result.Failure<ExpenseResponse>(ExpenseErrors.NotFound)
            : Result.Success(expense.ToResponse());
    }

    public async Task<IReadOnlyList<ExpenseResponse>> GetAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var expenses = await _repository.GetAllAsync(userId, cancellationToken);
        return expenses.Where(e => e.UserId == userId).Select(e => e.ToResponse()).ToList();
    }

    public async Task<Result<ExpenseResponse>> UpdateAsync(
        Guid userId, Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var expense = await FindOwnedAsync(userId, id, cancellationToken);
        if (expense is null)
        {
            return Result.Failure<ExpenseResponse>(ExpenseErrors.NotFound);
        }

        var updated = expense.Update(
            request.Description, request.Amount, request.Category, request.Date, _timeProvider.GetLocalNow());
        if (!updated.IsSuccess)
        {
            return Result.Failure<ExpenseResponse>(updated.Errors);
        }

        await _repository.UpdateAsync(expense, cancellationToken);
        return Result.Success(expense.ToResponse());
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(userId, id, cancellationToken);
        return deleted ? Result.Success() : Result.Failure(ExpenseErrors.NotFound);
    }

    // Missing and "owned by someone else" are the same answer, so ids of other users cannot be probed.
    private async Task<Expense?> FindOwnedAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(userId, id, cancellationToken);
        return expense is not null && expense.UserId == userId ? expense : null;
    }
}
