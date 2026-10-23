namespace Identity.Domain;

// Codes are part of the API contract (ProblemDetails); messages are shown to the user as they are.
public static class UserErrors
{
    public static readonly Error EmailInvalid = Error.Validation(
        "email", "user.email.invalid", $"Enter a valid email address (at most {User.EmailMaxLength} characters).");

    public static readonly Error DisplayNameLength = Error.Validation(
        "displayName",
        "user.display_name.length",
        $"Name must be between {User.DisplayNameMinLength} and {User.DisplayNameMaxLength} characters.");

    public static readonly Error PasswordLength = Error.Validation(
        "password",
        "user.password.length",
        $"Password must be between {User.PasswordMinLength} and {User.PasswordMaxLength} characters.");

    public static readonly Error PasswordUppercase = Error.Validation(
        "password", "user.password.uppercase", "Password must contain an upper case letter.");

    public static readonly Error PasswordLowercase = Error.Validation(
        "password", "user.password.lowercase", "Password must contain a lower case letter.");

    public static readonly Error PasswordDigit = Error.Validation(
        "password", "user.password.digit", "Password must contain a digit.");

    public static readonly Error PasswordSymbol = Error.Validation(
        "password", "user.password.symbol", "Password must contain a symbol.");

    public static readonly Error EmailTaken = Error.Conflict(
        "email", "user.email.taken", "An account with this email already exists.");

    // Same message for unknown email and wrong password, so the response does not reveal which accounts exist.
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "user.credentials.invalid", "Invalid email or password.");

    public static readonly Error NotAuthenticated = Error.Unauthorized(
        "user.not_authenticated", "The session is no longer valid. Sign in again.");
}
