using Identity.Application.Contracts;
using Identity.Application.Mapping;
using Identity.Domain;

namespace Identity.Application;

/// <summary>Register, login and "who am I". Rules live in <see cref="User"/>; this class orchestrates.</summary>
public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokens;
    private readonly TimeProvider _timeProvider;

    private static string? s_dummyHash;

    public AuthService(
        IUserRepository users, IPasswordHasher passwordHasher, ITokenService tokens, TimeProvider timeProvider)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var created = User.Create(
            request.Email, request.DisplayName, request.Password, _passwordHasher.Hash, _timeProvider.GetUtcNow());
        if (!created.IsSuccess)
        {
            return Result.Failure<AuthResponse>(created.Errors);
        }

        // The unique index decides, so two simultaneous registrations with the same email cannot both succeed.
        var user = created.Value!;
        if (!await _users.AddAsync(user, cancellationToken))
        {
            return Result.Failure<AuthResponse>(UserErrors.EmailTaken);
        }

        return Result.Success(IssueToken(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = User.NormalizeEmail(request.Email);
        var password = request.Password ?? string.Empty;
        if (email.Length == 0 || password.Length == 0)
        {
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
        }

        var user = await _users.GetByEmailAsync(email, cancellationToken);

        // Verify a password even when the email is unknown, so both failures take about the same time.
        var passwordMatches = _passwordHasher.Verify(user?.PasswordHash ?? GetDummyHash(), password);
        if (user is null || !passwordMatches)
        {
            return Result.Failure<AuthResponse>(UserErrors.InvalidCredentials);
        }

        return Result.Success(IssueToken(user));
    }

    public async Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        // A valid token for a user that no longer exists is a dead session, not a missing resource.
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? Result.Failure<UserResponse>(UserErrors.NotAuthenticated)
            : Result.Success(user.ToResponse());
    }

    private AuthResponse IssueToken(User user)
    {
        var token = _tokens.CreateToken(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, user.ToResponse());
    }

    // Computed once per process with the real hasher, so it has the same cost to verify as a stored hash.
    private string GetDummyHash() =>
        LazyInitializer.EnsureInitialized(ref s_dummyHash, () => _passwordHasher.Hash("not-a-real-password-Aa1!"));
}
