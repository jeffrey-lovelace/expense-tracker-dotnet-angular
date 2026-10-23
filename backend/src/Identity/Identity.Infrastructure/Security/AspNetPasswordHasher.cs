using Identity.Application;
using Identity.Domain;

using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Adapter from ASP.NET Core Identity's <see cref="PasswordHasher{TUser}"/> (PBKDF2 with HMAC-SHA512, a random
/// per-password salt and a versioned format) to <see cref="IPasswordHasher"/>. Only the hasher is used; there is no
/// EF or Identity store. Registered as a singleton: it holds no per-user state.
/// </summary>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    // The default PasswordHasher implementation never reads the user instance, so none is needed here.
    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password)
    {
        try
        {
            // SuccessRehashNeeded still means the password is correct; the stored hash is just an older format.
            return _inner.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // A corrupt stored hash must read as "wrong password", not as a 500.
            return false;
        }
    }
}
