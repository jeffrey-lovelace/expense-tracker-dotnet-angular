namespace Identity.Application.Contracts;

public sealed record LoginRequest(string Email, string Password)
{
    // See RegisterRequest: the password is masked so it can never be logged.
    public override string ToString() => $"LoginRequest {{ Email = {Email}, Password = *** }}";
}
