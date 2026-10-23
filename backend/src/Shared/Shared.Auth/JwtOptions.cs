using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace Shared.Auth;

/// <summary>
/// The "Jwt" configuration section. Both APIs read the same values: Identity.Api signs tokens with them and
/// Expenses.Api validates them. The signing key comes from appsettings.Development.json or an environment variable
/// (Jwt__SigningKey), never from appsettings.json.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 needs a key of at least 256 bits.</summary>
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int LifetimeMinutes { get; set; } = 60;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
