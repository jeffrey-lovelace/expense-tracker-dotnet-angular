using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Auth.Tests;

public sealed class JwtTokenServiceTests
{
    private static readonly Guid UserId = Guid.Parse("5d1c0b6e-8f2a-4c3e-9b7d-1a2b3c4d5e6f");
    private static readonly DateTimeOffset IssuedAt = new(2026, 10, 23, 2, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_ContainsSubEmailNameAndJti()
    {
        var token = JwtTestSetup.TokenService(IssuedAt).Issue(UserId, "demo@ballastlane.com", "Demo User");

        var jwt = new JsonWebToken(token.Value);
        Assert.Equal(UserId.ToString("D"), jwt.Subject);
        Assert.Equal("demo@ballastlane.com", jwt.GetClaim(JwtClaimNames.Email).Value);
        Assert.Equal("Demo User", jwt.GetClaim(JwtClaimNames.Name).Value);
        Assert.True(Guid.TryParse(jwt.Id, out _));
        Assert.Equal(JwtTestSetup.Issuer, jwt.Issuer);
        Assert.Equal([JwtTestSetup.Audience], jwt.Audiences);
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Alg);
    }

    [Fact]
    public void Issue_ExpiresAfterTheConfiguredLifetime()
    {
        var token = JwtTestSetup.TokenService(IssuedAt).Issue(UserId, "demo@ballastlane.com", "Demo User");

        var jwt = new JsonWebToken(token.Value);
        var expected = IssuedAt.UtcDateTime.AddMinutes(60);
        Assert.Equal(expected, token.ExpiresAtUtc);
        Assert.Equal(DateTimeKind.Utc, token.ExpiresAtUtc.Kind);
        Assert.Equal(expected, jwt.ValidTo);
        Assert.Equal(IssuedAt.UtcDateTime, jwt.IssuedAt);
    }

    [Fact]
    public void Issue_CalledTwice_UsesDifferentTokenIds()
    {
        var service = JwtTestSetup.TokenService(IssuedAt);

        var first = new JsonWebToken(service.Issue(UserId, "a@example.com", "A User").Value);
        var second = new JsonWebToken(service.Issue(UserId, "a@example.com", "A User").Value);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Issue_DoesNotPutTheSigningKeyInTheToken()
    {
        var token = JwtTestSetup.TokenService(IssuedAt).Issue(UserId, "demo@ballastlane.com", "Demo User");

        var jwt = new JsonWebToken(token.Value);
        Assert.DoesNotContain(JwtTestSetup.SigningKey, jwt.EncodedHeader + jwt.EncodedPayload, StringComparison.Ordinal);
    }
}
