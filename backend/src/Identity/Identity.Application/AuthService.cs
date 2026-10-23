using Identity.Application.Contracts;
using Identity.Domain;

namespace Identity.Application;

/// <summary>Register, login and "who am I". Rules live in <see cref="User"/>; this class orchestrates.</summary>
public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokens;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserRepository users, IPasswordHasher passwordHasher, ITokenService tokens, TimeProvider timeProvider)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _timeProvider = timeProvider;
    }

    public Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
