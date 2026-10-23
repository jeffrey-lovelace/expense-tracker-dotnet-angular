using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Web;

/// <summary>
/// Last line of defense: an exception nobody expected becomes a 500 ProblemDetails. The exception is logged on the
/// server; the response carries its details only in Development, so stack traces and SQL never reach a client.
/// Expected failures never get here: they are Result values mapped to 400/401/404/409.
/// </summary>
internal sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IProblemDetailsService problemDetails, IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The client went away: nothing to answer and nothing wrong with the server.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        // Method and path only: never the body (passwords) or the headers (tokens).
        LogUnhandled(exception, httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = _environment.IsDevelopment() ? exception.ToString() : null,
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private partial void LogUnhandled(Exception exception, string method, PathString path);
}
