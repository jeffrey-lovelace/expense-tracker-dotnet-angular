using Identity.Infrastructure.Security;

namespace Identity.Infrastructure.Tests;

public sealed class AspNetPasswordHasherTests
{
    private const string Password = "Demo123!";

    private readonly AspNetPasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotContainThePassword()
    {
        var hash = _hasher.Hash(Password);

        Assert.NotEqual(Password, hash);
        Assert.DoesNotContain(Password, hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Hash_UsesTheVersionedPbkdf2Format()
    {
        // ASP.NET Core Identity v3 format: first byte 0x01, then PRF, iteration count, salt size, salt and subkey.
        var bytes = Convert.FromBase64String(_hasher.Hash(Password));

        Assert.Equal(0x01, bytes[0]);
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentHashes()
    {
        // A random salt per hash: equal passwords cannot be spotted in the database.
        Assert.NotEqual(_hasher.Hash(Password), _hasher.Hash(Password));
    }

    [Fact]
    public void Verify_WithTheRightPassword_ReturnsTrue()
    {
        Assert.True(_hasher.Verify(_hasher.Hash(Password), Password));
    }

    [Theory]
    [InlineData("demo123!")]
    [InlineData("Demo123")]
    [InlineData("")]
    public void Verify_WithAnotherPassword_ReturnsFalse(string attempt)
    {
        Assert.False(_hasher.Verify(_hasher.Hash(Password), attempt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not base64 !!")]
    [InlineData("AAAA")]
    public void Verify_WithCorruptHash_ReturnsFalseInsteadOfThrowing(string corrupt)
    {
        Assert.False(_hasher.Verify(corrupt, Password));
    }
}
