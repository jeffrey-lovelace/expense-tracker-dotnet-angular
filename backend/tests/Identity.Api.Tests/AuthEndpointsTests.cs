using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.IdentityModel.JsonWebTokens;

namespace Identity.Api.Tests;

public sealed class AuthEndpointsTests : IClassFixture<IdentityApiFactory>
{
    private const string Password = "Demo123!";

    private readonly IdentityApiFactory _factory;

    public AuthEndpointsTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- Register ----------

    [Fact]
    public async Task Register_WithValidBody_Returns201WithTokenAndUser()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, displayName = "New User", password = Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/auth/me", response.Headers.Location!.OriginalString);
        var body = await ReadJsonAsync(response);
        var user = body.GetProperty("user");
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.Equal("New User", user.GetProperty("displayName").GetString());
        var token = new JsonWebToken(body.GetProperty("token").GetString());
        Assert.Equal(user.GetProperty("id").GetGuid().ToString("D"), token.Subject);
        Assert.True(body.GetProperty("expiresAtUtc").GetDateTime() > DateTime.UtcNow.AddMinutes(55));
    }

    [Fact]
    public async Task Register_ResponseNeverContainsThePasswordOrItsHash()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register", new { email = UniqueEmail(), displayName = "New User", password = Password });
        var raw = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(Password, raw, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WithTakenEmailInAnotherCase_Returns409()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register", new { email = email.ToUpperInvariant(), displayName = "Copy Cat", password = Password });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("user.email.taken", (await ReadJsonAsync(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_WithInvalidFields_Returns400WithEveryFieldError()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/register", new { email = "nope", displayName = "x", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("email", out _));
        Assert.True(errors.TryGetProperty("displayName", out _));
        Assert.True(errors.TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Register_WithMissingFields_Returns400KeyedByJsonName()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new { email = UniqueEmail() });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("displayName", out _), errors.ToString());
        Assert.True(errors.TryGetProperty("password", out _), errors.ToString());
    }

    // ---------- Login ----------

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200AndTheTokenOpensMe()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        using var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await ReadJsonAsync(login)).GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(email, (await ReadJsonAsync(me)).GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_WithEmailInAnotherCase_Returns200()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        using var login = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = $"  {email.ToUpperInvariant()} ", password = Password });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401Generic()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("Invalid email or password.", (await ReadJsonAsync(response)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ReturnTheSameBody()
    {
        using var client = _factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        using var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong123!" });
        using var unknownEmail = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = UniqueEmail(), password = Password });

        Assert.Equal(wrongPassword.StatusCode, unknownEmail.StatusCode);
        var first = await ReadJsonAsync(wrongPassword);
        var second = await ReadJsonAsync(unknownEmail);
        foreach (var field in new[] { "status", "title", "detail", "code" })
        {
            Assert.Equal(first.GetProperty(field).ToString(), second.GetProperty(field).ToString());
        }
    }

    // ---------- Me ----------

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, string> BrokenTokens => new()
    {
        { "expired", TestJwt.For(Guid.NewGuid(), expiresUtc: DateTime.UtcNow.AddMinutes(-5)) },
        { "wrong signature", TestJwt.For(Guid.NewGuid(), signingKey: "another-key-that-is-long-enough-0123456789") },
        { "wrong issuer", TestJwt.For(Guid.NewGuid(), issuer: "someone-else") },
        { "wrong audience", TestJwt.For(Guid.NewGuid(), audience: "another-api") },
        { "no sub", TestJwt.For(Guid.NewGuid(), includeSub: false) },
    };

    [Theory]
    [MemberData(nameof(BrokenTokens))]
    public async Task Me_WithInvalidToken_Returns401(string reason, string token)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/auth/me");

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, reason);
    }

    [Fact]
    public async Task Me_WithValidTokenForUnknownUser_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.For(Guid.NewGuid()));

        using var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Health ----------

    [Fact]
    public async Task Health_WithoutToken_Returns200Healthy()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", (await ReadJsonAsync(response)).GetProperty("status").GetString());
    }

    // ---------- Helpers ----------

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, displayName = "Test User", password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
