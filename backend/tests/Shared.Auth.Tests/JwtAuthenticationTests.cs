using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Auth.Tests;

// The issuing side (JwtTokenService) and the validating side (AddJwtAuthentication) must agree. These tests run a
// real token through the exact validation parameters the APIs use.
public sealed class JwtAuthenticationTests
{
    private static readonly Guid UserId = Guid.Parse("5d1c0b6e-8f2a-4c3e-9b7d-1a2b3c4d5e6f");

    [Fact]
    public async Task IssuedToken_IsAcceptedByTheSharedValidation()
    {
        using var services = JwtTestSetup.BuildServices(JwtTestSetup.ValidSettings());
        var token = JwtTestSetup.TokenService(DateTimeOffset.UtcNow).Issue(UserId, "demo@ballastlane.com", "Demo User");

        var result = await Validate(services, token.Value);

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal(UserId.ToString("D"), result.ClaimsIdentity.FindFirst(JwtClaimNames.Subject)?.Value);
    }

    [Fact]
    public async Task TokenExpiredWithinClockSkew_IsStillAccepted()
    {
        using var services = JwtTestSetup.BuildServices(JwtTestSetup.ValidSettings());
        var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-60).AddSeconds(-10); // expired 10 s ago, skew is 30 s
        var token = JwtTestSetup.TokenService(issuedAt).Issue(UserId, "demo@ballastlane.com", "Demo User");

        var result = await Validate(services, token.Value);

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public async Task TokenExpiredBeyondClockSkew_IsRejected()
    {
        using var services = JwtTestSetup.BuildServices(JwtTestSetup.ValidSettings());
        var issuedAt = DateTimeOffset.UtcNow.AddMinutes(-60).AddSeconds(-60); // expired 60 s ago
        var token = JwtTestSetup.TokenService(issuedAt).Issue(UserId, "demo@ballastlane.com", "Demo User");

        var result = await Validate(services, token.Value);

        Assert.False(result.IsValid);
        Assert.IsType<SecurityTokenExpiredException>(result.Exception);
    }

    [Fact]
    public async Task TokenSignedWithAnotherKey_IsRejected()
    {
        using var services = JwtTestSetup.BuildServices(JwtTestSetup.ValidSettings());
        var token = JwtTestSetup.TokenService(DateTimeOffset.UtcNow, "an-attackers-key-that-is-long-enough-012345")
            .Issue(UserId, "demo@ballastlane.com", "Demo User");

        var result = await Validate(services, token.Value);

        Assert.False(result.IsValid);
        Assert.IsAssignableFrom<SecurityTokenSignatureKeyNotFoundException>(result.Exception);
    }

    [Fact]
    public async Task UnsignedToken_IsRejected()
    {
        using var services = JwtTestSetup.BuildServices(JwtTestSetup.ValidSettings());
        var unsigned = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = JwtTestSetup.Issuer,
            Audience = JwtTestSetup.Audience,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object> { [JwtClaimNames.Subject] = UserId.ToString("D") },
        });

        var result = await Validate(services, unsigned);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Jwt:SigningKey", null, "SigningKey")]
    [InlineData("Jwt:SigningKey", "0123456789012345678901234567890", "SigningKey")] // 31 bytes
    [InlineData("Jwt:Issuer", "", "Issuer")]
    [InlineData("Jwt:Audience", "", "Audience")]
    [InlineData("Jwt:LifetimeMinutes", "0", "LifetimeMinutes")]
    public void Options_WithInvalidValue_FailValidation(string key, string? value, string expectedInMessage)
    {
        var settings = JwtTestSetup.ValidSettings();
        settings[key] = value;
        using var services = JwtTestSetup.BuildServices(settings);

        var exception = Assert.Throws<OptionsValidationException>(
            () => services.GetRequiredService<IOptions<JwtOptions>>().Value);

        Assert.Contains(expectedInMessage, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Options_KeyLengthIsMeasuredInBytes()
    {
        // 16 characters, but 32 bytes in UTF-8: accepted, because HMAC uses the bytes.
        var settings = JwtTestSetup.ValidSettings();
        settings["Jwt:SigningKey"] = new string('ñ', 16);
        using var services = JwtTestSetup.BuildServices(settings);

        var options = services.GetRequiredService<IOptions<JwtOptions>>().Value;

        Assert.Equal(16, options.SigningKey.Length);
    }

    private static Task<TokenValidationResult> Validate(ServiceProvider services, string token) =>
        new JsonWebTokenHandler().ValidateTokenAsync(token, JwtTestSetup.ValidationParameters(services));
}
