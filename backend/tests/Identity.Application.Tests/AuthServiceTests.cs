using Identity.Application.Contracts;
using Identity.Domain;

using Moq;

namespace Identity.Application.Tests;

// Field rules are tested in Identity.Domain.Tests. These tests cover orchestration: hashing, saving, the duplicate
// email, the generic login failure, and that a token is issued only on success.
public sealed class AuthServiceTests
{
    private const string Password = "Demo123!";
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 22, 0, 0, TimeSpan.FromHours(-4));
    private static readonly DateTime TokenExpiry = new(2026, 10, 23, 3, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => $"hash:{p}");
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((hash, p) => hash == $"hash:{p}");
        _tokens.Setup(t => t.CreateToken(It.IsAny<User>()))
            .Returns<User>(u => new IssuedToken($"token-for-{u.Id}", TokenExpiry));
        _service = new AuthService(_users.Object, _hasher.Object, _tokens.Object, new FixedClock(Now));
    }

    // ---------- Register ----------

    [Fact]
    public async Task RegisterAsync_WithValidRequest_SavesHashedUserAndReturnsToken()
    {
        User? saved = null;
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => saved = u)
            .ReturnsAsync(true);

        var result = await _service.RegisterAsync(
            new RegisterRequest(" New@Example.com ", "New User", Password), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(saved);
        Assert.Equal("new@example.com", saved.Email);
        Assert.Equal("hash:Demo123!", saved.PasswordHash);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAtUtc);
        var response = result.Value!;
        Assert.Equal($"token-for-{saved.Id}", response.Token);
        Assert.Equal(TokenExpiry, response.ExpiresAtUtc);
        Assert.Equal(new UserResponse(saved.Id, "new@example.com", "New User"), response.User);
        _hasher.Verify(h => h.Hash(Password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WhenDomainRejectsRequest_ReturnsErrorsAndSavesNothing()
    {
        var result = await _service.RegisterAsync(
            new RegisterRequest("not-an-email", "New User", Password), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("email", Assert.Single(result.Errors).Field);
        _users.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithTakenEmail_ReturnsConflictAndNoToken()
    {
        _users.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _service.RegisterAsync(
            new RegisterRequest("demo@ballastlane.com", "Demo User", Password), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Conflict, error.Type);
        Assert.Equal("email", error.Field);
        _tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
    }

    // ---------- Login ----------

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsTokenForThatUser()
    {
        var user = ExistingUser();
        _users.Setup(r => r.GetByEmailAsync("demo@ballastlane.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest("demo@ballastlane.com", Password), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal($"token-for-{user.Id}", result.Value!.Token);
        Assert.Equal(user.Id, result.Value.User.Id);
    }

    [Fact]
    public async Task LoginAsync_NormalizesEmailBeforeLookup()
    {
        var user = ExistingUser();
        _users.Setup(r => r.GetByEmailAsync("demo@ballastlane.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.LoginAsync(
            new LoginRequest("  DEMO@BallastLane.com ", Password), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsGenericUnauthorized()
    {
        _users.Setup(r => r.GetByEmailAsync("demo@ballastlane.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingUser());

        var result = await _service.LoginAsync(
            new LoginRequest("demo@ballastlane.com", "Wrong123!"), CancellationToken.None);

        AssertInvalidCredentials(result);
        _tokens.Verify(t => t.CreateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsTheSameErrorAsWrongPassword()
    {
        var unknown = await _service.LoginAsync(new LoginRequest("nobody@example.com", Password), CancellationToken.None);
        _users.Setup(r => r.GetByEmailAsync("demo@ballastlane.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingUser());
        var wrongPassword = await _service.LoginAsync(
            new LoginRequest("demo@ballastlane.com", "Wrong123!"), CancellationToken.None);

        AssertInvalidCredentials(unknown);
        Assert.Equal(wrongPassword.Errors, unknown.Errors);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_StillVerifiesAPassword()
    {
        // Otherwise "unknown email" answers faster than "wrong password" and the timing reveals which accounts exist.
        await _service.LoginAsync(new LoginRequest("nobody@example.com", Password), CancellationToken.None);

        _hasher.Verify(h => h.Verify(It.IsAny<string>(), Password), Times.Once);
    }

    [Theory]
    [InlineData("", "Demo123!")]
    [InlineData("demo@ballastlane.com", "")]
    [InlineData(null, null)]
    public async Task LoginAsync_WithMissingFields_ReturnsGenericUnauthorized(string? email, string? password)
    {
        var result = await _service.LoginAsync(new LoginRequest(email!, password!), CancellationToken.None);

        AssertInvalidCredentials(result);
    }

    // ---------- Current user ----------

    [Fact]
    public async Task GetCurrentUserAsync_WithExistingUser_ReturnsIt()
    {
        var user = ExistingUser();
        _users.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _service.GetCurrentUserAsync(user.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new UserResponse(user.Id, user.Email, user.DisplayName), result.Value);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUserNoLongerExists_ReturnsUnauthorized()
    {
        var result = await _service.GetCurrentUserAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, Assert.Single(result.Errors).Type);
    }

    // ---------- Helpers ----------

    private static User ExistingUser() =>
        User.Rehydrate(
            Guid.Parse("5d1c0b6e-8f2a-4c3e-9b7d-1a2b3c4d5e6f"),
            "demo@ballastlane.com",
            "Demo User",
            $"hash:{Password}",
            new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc));

    private static void AssertInvalidCredentials(Result<AuthResponse> result)
    {
        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Unauthorized, error.Type);
        Assert.Equal("user.credentials.invalid", error.Code);
        Assert.Equal("Invalid email or password.", error.Message);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();

        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone("UTC-04", now.Offset, "UTC-04", "UTC-04");
    }
}
