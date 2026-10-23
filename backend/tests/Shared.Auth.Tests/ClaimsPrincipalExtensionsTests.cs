using System.Security.Claims;

namespace Shared.Auth.Tests;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void TryGetUserId_WithSubClaim_ReturnsIt()
    {
        var id = Guid.NewGuid();

        var found = Principal(new Claim(JwtClaimNames.Subject, id.ToString("D"))).TryGetUserId(out var userId);

        Assert.True(found);
        Assert.Equal(id, userId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void TryGetUserId_WithMissingOrInvalidSub_ReturnsFalse(string? subject)
    {
        var claims = subject is null ? [] : new[] { new Claim(JwtClaimNames.Subject, subject) };

        Assert.False(Principal(claims).TryGetUserId(out _));
    }

    [Fact]
    public void TryGetUserId_IgnoresTheMappedNameIdentifierClaim()
    {
        // Only "sub" counts. If claim mapping were turned back on, this would surface as a test failure.
        var principal = Principal(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")));

        Assert.False(principal.TryGetUserId(out _));
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "Test"));
}
