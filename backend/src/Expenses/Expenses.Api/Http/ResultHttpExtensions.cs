using Expenses.Domain;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Expenses.Api.Http;

/// <summary>The one place where expected failures become HTTP responses (RFC 7807 ProblemDetails).</summary>
internal static class ResultHttpExtensions
{
    public static ActionResult Problem(this ControllerBase controller, IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(errors);

        var first = errors[0];
        if (first.Type == ErrorType.Validation)
        {
            // Keyed by the camelCase JSON field name, so Angular maps each message to its form control.
            var modelState = new ModelStateDictionary();
            foreach (var error in errors)
            {
                modelState.AddModelError(error.Field ?? string.Empty, error.Message);
            }

            return controller.ValidationProblem(modelState);
        }

        var status = first.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext, statusCode: status, detail: first.Message);
        problem.Extensions["code"] = first.Code;
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
