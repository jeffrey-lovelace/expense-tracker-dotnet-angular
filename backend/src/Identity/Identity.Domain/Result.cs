namespace Identity.Domain;

// IsSuccess is a stored flag, not "no errors", so default(Result) is a failure instead of a silent success.
// A failure carries every error found (AGENTS.md 10.10), so the form can show all invalid fields at once.
// The generic factories live on the non-generic Result (CA1000): Result.Success(value), Result.Failure<T>(errors).

public readonly record struct Result
{
    private static readonly IReadOnlyList<Error> NoErrors = [];

    private readonly IReadOnlyList<Error>? _errors;

    private Result(bool isSuccess, IReadOnlyList<Error>? errors)
    {
        IsSuccess = isSuccess;
        _errors = errors;
    }

    public bool IsSuccess { get; }

    public IReadOnlyList<Error> Errors => _errors ?? NoErrors;

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => Failure([error]);

    public static Result Failure(IReadOnlyList<Error> errors) => new(false, EnsureErrors(errors));

    public static Result<T> Success<T>(T value) => new(true, value, null);

    public static Result<T> Failure<T>(Error error) => Failure<T>([error]);

    public static Result<T> Failure<T>(IReadOnlyList<Error> errors) => new(false, default, EnsureErrors(errors));

    internal static IReadOnlyList<Error> EnsureErrors(IReadOnlyList<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Count == 0)
        {
            throw new ArgumentException("A failure needs at least one error.", nameof(errors));
        }

        return errors;
    }
}

public readonly record struct Result<T>
{
    private static readonly IReadOnlyList<Error> NoErrors = [];

    private readonly IReadOnlyList<Error>? _errors;

    internal Result(bool isSuccess, T? value, IReadOnlyList<Error>? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        _errors = errors;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public IReadOnlyList<Error> Errors => _errors ?? NoErrors;
}
