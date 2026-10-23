using Identity.Domain;
using Identity.Infrastructure.Security;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

using Shared.Auth;

namespace Identity.Infrastructure.Tests;

public sealed class JwtUserTokenServiceTests
{
    [Fact]
    public void CreateToken_PutsTheUserInTheClaims()
    {
        var now = new DateTimeOffset(2026, 10, 23, 2, 0, 0, TimeSpan.Zero);
        var jwt = new JwtTokenService(
            Options.Create(new JwtOptions
            {
                Issuer = "expense-tracker-identity",
                Audience = "expense-tracker",
                SigningKey = "test-only-signing-key-0123456789-abcdefghij",
                LifetimeMinutes = 60,
            }),
            new FixedClock(now));
        var user = User.Rehydrate(
            Guid.NewGuid(), "demo@ballastlane.com", "Demo User", "hash", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));

        var issued = new JwtUserTokenService(jwt).CreateToken(user);

        var token = new JsonWebToken(issued.Token);
        Assert.Equal(user.Id.ToString("D"), token.Subject);
        Assert.Equal("demo@ballastlane.com", token.GetClaim(JwtClaimNames.Email).Value);
        Assert.Equal("Demo User", token.GetClaim(JwtClaimNames.Name).Value);
        Assert.Equal(now.UtcDateTime.AddHours(1), issued.ExpiresAtUtc);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
