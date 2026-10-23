namespace Shared.Auth;

/// <summary>The claims our tokens carry. Inbound claim mapping is off, so these are the names the APIs see.</summary>
public static class JwtClaimNames
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string TokenId = "jti";
}
