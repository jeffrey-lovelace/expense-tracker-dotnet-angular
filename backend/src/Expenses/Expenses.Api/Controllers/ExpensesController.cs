using Expenses.Api.Http;
using Expenses.Application;
using Expenses.Application.Contracts;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Shared.Auth;

namespace Expenses.Api.Controllers;

/// <summary>
/// CRUD over the caller's expenses. HTTP in, service call, Result to HTTP out. The user id comes only from the
/// "sub" claim of the validated token; no action accepts it from the body, the route or the query string.
/// </summary>
[ApiController]
[Route("api/expenses")]
[Authorize]
public sealed class ExpensesController : ControllerBase
{
    private readonly ExpenseService _service;

    public ExpensesController(ExpenseService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType<ExpenseListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseListResponse>> List(
        [FromQuery] ExpenseListQuery query, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.ListAsync(userId, query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Problem(result.Errors);
    }

    [HttpGet("{id:guid}", Name = "GetExpense")]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.GetByIdAsync(userId, id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Problem(result.Errors);
    }

    [HttpPost]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseResponse>> Create(
        CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.CreateAsync(userId, request, cancellationToken);
        return result.IsSuccess
            ? CreatedAtRoute("GetExpense", new { id = result.Value!.Id }, result.Value)
            : this.Problem(result.Errors);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseResponse>> Update(
        Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.UpdateAsync(userId, id, request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.Problem(result.Errors);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await _service.DeleteAsync(userId, id, cancellationToken);
        return result.IsSuccess ? NoContent() : this.Problem(result.Errors);
    }
}
