namespace Identity.Domain;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
}

/// <summary>An expected failure. <see cref="Field"/> names the request field for validation errors.</summary>
public sealed record Error(ErrorType Type, string Code, string Message, string? Field = null)
{
    public static Error Validation(string field, string code, string message) =>
        new(ErrorType.Validation, code, message, field);

    public static Error NotFound(string code, string message) =>
        new(ErrorType.NotFound, code, message);

    public static Error Conflict(string field, string code, string message) =>
        new(ErrorType.Conflict, code, message, field);

    public static Error Unauthorized(string code, string message) =>
        new(ErrorType.Unauthorized, code, message);
}
