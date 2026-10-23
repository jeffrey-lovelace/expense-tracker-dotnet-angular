using System.Security.Claims;

namespace Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Reads the user id from the "sub" claim of the validated token. This is the only source of the user id:
    /// never the request body, the route or the query string.
    /// </summary>
    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var subject = principal.FindFirst(JwtClaimNames.Subject)?.Value;
        return Guid.TryParse(subject, out userId) && userId != Guid.Empty;
    }
}
