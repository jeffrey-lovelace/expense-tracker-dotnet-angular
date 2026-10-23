using System.Security.Claims;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Auth;

/// <summary>
/// Issues HS256 tokens with the claims both APIs rely on: sub (user id), email, name and jti, valid for
/// <see cref="JwtOptions.LifetimeMinutes"/>. Registered as a singleton: it only holds immutable options and
/// signing credentials, and <see cref="JsonWebTokenHandler"/> is thread-safe.
/// </summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TimeProvider _timeProvider;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _signingCredentials = new SigningCredentials(_options.CreateSigningKey(), SecurityAlgorithms.HmacSha256);
        _timeProvider = timeProvider;
    }

    public JwtToken Issue(Guid userId, string email, string name)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.LifetimeMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtClaimNames.Subject, userId.ToString("D")),
                new Claim(JwtClaimNames.Email, email),
                new Claim(JwtClaimNames.Name, name),
                new Claim(JwtClaimNames.TokenId, Guid.NewGuid().ToString("D")),
            ]),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = _signingCredentials,
        };

        return new JwtToken(_handler.CreateToken(descriptor), expires);
    }
}

public sealed record JwtToken(string Value, DateTime ExpiresAtUtc);
