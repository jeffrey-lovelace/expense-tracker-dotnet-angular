using System.Security.Claims;
using System.Text;

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Expenses.Api.Tests;

/// <summary>Mints tokens the way Identity.Api does, plus broken variants for the 401 tests.</summary>
internal static class TestJwt
{
    public const string Issuer = "expense-tracker-identity";
    public const string Audience = "expense-tracker";
    public const string SigningKey = "test-only-signing-key-0123456789-abcdefghij";

    public static string For(
        Guid userId,
        string signingKey = SigningKey,
        string issuer = Issuer,
        string audience = Audience,
        DateTime? expiresUtc = null,
        bool includeSub = true)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Email, "user@example.com") };
        if (includeSub)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, userId.ToString("D")));
        }

        var expires = expiresUtc ?? DateTime.UtcNow.AddMinutes(60);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = issuer,
            Audience = audience,
            NotBefore = expires.AddHours(-2),
            IssuedAt = expires.AddHours(-2),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
