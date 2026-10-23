using System.Diagnostics.CodeAnalysis;

namespace Expenses.Domain;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
}

/// <summary>An expected failure. <see cref="Field"/> names the request field for validation errors.</summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "'Error' is only a keyword in Visual Basic; this solution is C# only and the name is the domain term.")]
public sealed record Error(ErrorType Type, string Code, string Message, string? Field = null)
{
    public static Error Validation(string field, string code, string message) =>
        new(ErrorType.Validation, code, message, field);

    public static Error NotFound(string code, string message) =>
        new(ErrorType.NotFound, code, message);
}
