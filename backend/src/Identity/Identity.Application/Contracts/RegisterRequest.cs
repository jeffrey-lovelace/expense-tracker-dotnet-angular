namespace Identity.Application.Contracts;

public sealed record RegisterRequest(string Email, string DisplayName, string Password)
{
    // A record prints every property, and ASP.NET logs action arguments at Trace level. The password must never
    // reach a log, so it is masked here (caught by HostingTests.Logs_NeverContainPasswordsOrTokens).
    public override string ToString() => $"RegisterRequest {{ Email = {Email}, DisplayName = {DisplayName}, Password = *** }}";
}
