using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Identity.Api.Tests;

public sealed class SeedTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;

    public SeedTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("demo@ballastlane.com", "5d1c0b6e-8f2a-4c3e-9b7d-1a2b3c4d5e6f")]
    [InlineData("other@ballastlane.com", "a8f3e2d1-6b4c-4f7a-8e9d-0c1b2a3f4e5d")]
    public async Task SeededUser_CanLogInWithTheDemoPassword(string email, string expectedId)
    {
        using var seeded = _factory.WithWebHostBuilder(builder => builder.UseSetting("Seed:Enabled", "true"));
        using var client = seeded.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Demo123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString();
        Assert.Equal(expectedId, new JsonWebToken(token).Subject);
    }
}
