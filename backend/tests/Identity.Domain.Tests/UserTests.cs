namespace Identity.Domain.Tests;

public sealed class UserTests
{
    private const string ValidPassword = "Demo123!";
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 22, 0, 0, TimeSpan.FromHours(-4));

    // Deterministic stand-in for PBKDF2, so tests can see what was hashed.
    private static string FakeHash(string password) => $"hashed:{password}";

    // ---------- Valid ----------

    [Fact]
    public void Create_WithValidValues_ReturnsUserWithHashedPassword()
    {
        var result = User.Create("demo@ballastlane.com", "Demo User", ValidPassword, FakeHash, Now);

        Assert.True(result.IsSuccess);
        var user = result.Value!;
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("demo@ballastlane.com", user.Email);
        Assert.Equal("Demo User", user.DisplayName);
        Assert.Equal("hashed:Demo123!", user.PasswordHash);
        Assert.Equal(Now.UtcDateTime, user.CreatedAtUtc);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAtUtc.Kind);
    }

    [Fact]
    public void Create_NormalizesEmailAndTrimsDisplayName()
    {
        var result = User.Create("  Demo@BallastLane.COM ", "  Demo User  ", ValidPassword, FakeHash, Now);

        Assert.Equal("demo@ballastlane.com", result.Value!.Email);
        Assert.Equal("Demo User", result.Value.DisplayName);
    }

    [Fact]
    public void Create_DoesNotTrimThePassword()
    {
        // Spaces are characters the user chose; trimming would silently change their password.
        var result = User.Create("demo@ballastlane.com", "Demo User", " Demo123! ", FakeHash, Now);

        Assert.Equal("hashed: Demo123! ", result.Value!.PasswordHash);
    }

    [Theory]
    [InlineData("  Demo@Example.COM  ", "demo@example.com")]
    [InlineData("a@b.co", "a@b.co")]
    [InlineData(null, "")]
    public void NormalizeEmail_TrimsAndLowerCases(string? input, string expected)
    {
        Assert.Equal(expected, User.NormalizeEmail(input));
    }

    // ---------- Email ----------

    public static TheoryData<string> InvalidEmails => new()
    {
        null!,
        string.Empty,
        "   ",
        "plainaddress",
        "@missing-local.com",
        "missing-at.com",
        "user@",
        "user@domain",
        "user name@domain.com",
        "user@@domain.com",
        new string('a', User.EmailMaxLength - "@example.com".Length + 1) + "@example.com",
    };

    [Theory]
    [MemberData(nameof(InvalidEmails))]
    public void Create_WithInvalidEmail_ReturnsEmailError(string email)
    {
        var result = User.Create(email, "Demo User", ValidPassword, FakeHash, Now);

        AssertOnlyErrors(result, "email", "user.email.invalid");
    }

    [Fact]
    public void Create_WithEmailAtMaxLength_Succeeds()
    {
        var email = new string('a', User.EmailMaxLength - "@example.com".Length) + "@example.com";

        var result = User.Create(email, "Demo User", ValidPassword, FakeHash, Now);

        Assert.True(result.IsSuccess);
    }

    // ---------- Display name ----------

    public static TheoryData<string> InvalidDisplayNames => new()
    {
        null!,
        string.Empty,
        "    ",
        "a",
        "  a  ",
        new string('x', User.DisplayNameMaxLength + 1),
    };

    [Theory]
    [MemberData(nameof(InvalidDisplayNames))]
    public void Create_WithInvalidDisplayName_ReturnsDisplayNameError(string displayName)
    {
        var result = User.Create("demo@ballastlane.com", displayName, ValidPassword, FakeHash, Now);

        AssertOnlyErrors(result, "displayName", "user.display_name.length");
    }

    [Theory]
    [InlineData("Al")]
    [InlineData("  Al  ")]
    public void Create_WithDisplayNameAtMinLength_Succeeds(string displayName)
    {
        var result = User.Create("demo@ballastlane.com", displayName, ValidPassword, FakeHash, Now);

        Assert.True(result.IsSuccess);
    }

    // ---------- Password ----------

    [Theory]
    [InlineData("Ab1!xyz", "user.password.length")]           // 7 characters
    [InlineData("abcdefg1!", "user.password.uppercase")]
    [InlineData("ABCDEFG1!", "user.password.lowercase")]
    [InlineData("Abcdefgh!", "user.password.digit")]
    [InlineData("Abcdefgh1", "user.password.symbol")]
    public void Create_WithWeakPassword_ReturnsThatPasswordError(string password, string expectedCode)
    {
        var result = User.Create("demo@ballastlane.com", "Demo User", password, FakeHash, Now);

        AssertOnlyErrors(result, "password", expectedCode);
    }

    [Fact]
    public void Create_WithTooLongPassword_ReturnsLengthError()
    {
        var password = "Aa1!" + new string('x', User.PasswordMaxLength - 3);

        var result = User.Create("demo@ballastlane.com", "Demo User", password, FakeHash, Now);

        AssertOnlyErrors(result, "password", "user.password.length");
    }

    [Fact]
    public void Create_WithEmptyPassword_ReportsEveryMissingRequirement()
    {
        string[] expectedCodes =
        [
            "user.password.digit",
            "user.password.length",
            "user.password.lowercase",
            "user.password.symbol",
            "user.password.uppercase",
        ];

        var result = User.Create("demo@ballastlane.com", "Demo User", string.Empty, FakeHash, Now);

        Assert.False(result.IsSuccess);
        Assert.All(result.Errors, e => Assert.Equal("password", e.Field));
        Assert.Equal(expectedCodes, result.Errors.Select(e => e.Code).Order());
    }

    [Theory]
    [InlineData("Demo123!")]
    [InlineData("Pässwörd1#")]  // non-ASCII letters count as letters
    [InlineData("Abcdef1 ")]    // a space is a symbol: anything that is not a letter or digit
    public void Create_WithStrongPassword_Succeeds(string password)
    {
        var result = User.Create("demo@ballastlane.com", "Demo User", password, FakeHash, Now);

        Assert.True(result.IsSuccess);
    }

    // ---------- Several errors and hashing ----------

    [Fact]
    public void Create_WithSeveralInvalidFields_ReturnsEveryFieldAndDoesNotHash()
    {
        var hashCalls = 0;
        string CountingHash(string p)
        {
            hashCalls++;
            return p;
        }

        var result = User.Create("not-an-email", "x", "short", CountingHash, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(["displayName", "email", "password"], result.Errors.Select(e => e.Field).Distinct().Order());
        Assert.Equal(0, hashCalls);
    }

    [Fact]
    public void Create_CalledTwice_GeneratesDistinctIds()
    {
        var first = User.Create("a@example.com", "Demo User", ValidPassword, FakeHash, Now);
        var second = User.Create("a@example.com", "Demo User", ValidPassword, FakeHash, Now);

        Assert.NotEqual(first.Value!.Id, second.Value!.Id);
    }

    // ---------- Helpers ----------

    private static void AssertOnlyErrors(Result<User> result, string field, string code)
    {
        Assert.False(result.IsSuccess);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(field, error.Field);
        Assert.Equal(code, error.Code);
    }
}
