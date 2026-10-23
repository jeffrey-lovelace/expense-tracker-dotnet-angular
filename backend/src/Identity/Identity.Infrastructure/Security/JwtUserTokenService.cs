using Identity.Application;
using Identity.Domain;

using Shared.Auth;

namespace Identity.Infrastructure.Security;

/// <summary>Adapter from the shared <see cref="JwtTokenService"/> to the Identity port <see cref="ITokenService"/>.</summary>
public sealed class JwtUserTokenService : ITokenService
{
    private readonly JwtTokenService _jwt;

    public JwtUserTokenService(JwtTokenService jwt)
    {
        _jwt = jwt;
    }

    public IssuedToken CreateToken(User user) => throw new NotImplementedException();
}
