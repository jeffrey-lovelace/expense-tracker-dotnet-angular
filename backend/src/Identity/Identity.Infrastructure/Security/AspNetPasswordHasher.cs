using Identity.Application;

namespace Identity.Infrastructure.Security;

/// <summary>
/// Adapter from ASP.NET Core Identity's <c>PasswordHasher</c> (PBKDF2 with a random per-password salt, versioned
/// format) to <see cref="IPasswordHasher"/>. Only the hasher is used; there is no EF or Identity store.
/// </summary>
public sealed class AspNetPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => throw new NotImplementedException();

    public bool Verify(string passwordHash, string password) => throw new NotImplementedException();
}
