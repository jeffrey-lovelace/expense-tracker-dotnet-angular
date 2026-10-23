using Microsoft.Extensions.Options;

namespace Shared.Auth;

/// <summary>
/// Issues HS256 tokens with the claims both APIs rely on: sub (user id), email, name and jti, valid for
/// <see cref="JwtOptions.LifetimeMinutes"/>. Registered as a singleton: it only holds immutable options.
/// </summary>
public sealed class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public JwtToken Issue(Guid userId, string email, string name) => throw new NotImplementedException();
}

public sealed record JwtToken(string Value, DateTime ExpiresAtUtc);
