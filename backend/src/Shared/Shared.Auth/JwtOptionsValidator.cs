using System.Text;

using Microsoft.Extensions.Options;

namespace Shared.Auth;

/// <summary>Runs at startup (ValidateOnStart): the API refuses to start with a missing or short key.</summary>
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        if (Encoding.UTF8.GetByteCount(options.SigningKey ?? string.Empty) < JwtOptions.MinSigningKeyBytes)
        {
            failures.Add($"Jwt:SigningKey must be at least {JwtOptions.MinSigningKeyBytes} bytes.");
        }

        if (options.LifetimeMinutes is < 1 or > 24 * 60)
        {
            failures.Add("Jwt:LifetimeMinutes must be between 1 and 1440.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
