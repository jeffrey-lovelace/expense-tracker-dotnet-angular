using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Auth.Tests;

internal static class JwtTestSetup
{
    public const string Issuer = "expense-tracker-identity";
    public const string Audience = "expense-tracker";
    public const string SigningKey = "test-only-signing-key-0123456789-abcdefghij";

    public static Dictionary<string, string?> ValidSettings() => new()
    {
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
        ["Jwt:SigningKey"] = SigningKey,
        ["Jwt:LifetimeMinutes"] = "60",
    };

    /// <summary>Builds the container exactly as both APIs do, through <c>AddJwtAuthentication</c>.</summary>
    public static ServiceProvider BuildServices(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection()
            .AddLogging()
            .AddJwtAuthentication(configuration)
            .BuildServiceProvider();
    }

    /// <summary>The validation parameters the APIs use at runtime.</summary>
    public static TokenValidationParameters ValidationParameters(ServiceProvider services) =>
        services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters;

    public static JwtTokenService TokenService(DateTimeOffset now, string signingKey = SigningKey) =>
        new(
            Options.Create(new JwtOptions
            {
                Issuer = Issuer,
                Audience = Audience,
                SigningKey = signingKey,
                LifetimeMinutes = 60,
            }),
            new FixedClock(now));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
