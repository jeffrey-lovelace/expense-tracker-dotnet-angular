using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;

namespace Expenses.Api.Tests;

public sealed class SeedTests : IClassFixture<ExpensesApiFactory>
{
    private static readonly Guid DemoId = Guid.Parse("5d1c0b6e-8f2a-4c3e-9b7d-1a2b3c4d5e6f");
    private static readonly Guid OtherId = Guid.Parse("a8f3e2d1-6b4c-4f7a-8e9d-0c1b2a3f4e5d");

    private readonly ExpensesApiFactory _factory;

    public SeedTests(ExpensesApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeededUsers_SeeTheirOwnSampleExpenses()
    {
        using var seeded = _factory.WithWebHostBuilder(builder => builder.UseSetting("Seed:Enabled", "true"));
        using var demo = seeded.CreateClient();
        demo.DefaultRequestHeaders.Authorization = new("Bearer", TestJwt.For(DemoId));
        using var other = seeded.CreateClient();
        other.DefaultRequestHeaders.Authorization = new("Bearer", TestJwt.For(OtherId));

        var demoPage = await demo.GetFromJsonAsync<JsonElement>("/api/expenses?pageSize=100");
        var otherPage = await other.GetFromJsonAsync<JsonElement>("/api/expenses?pageSize=100");

        Assert.Equal(30, demoPage.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, otherPage.GetProperty("totalCount").GetInt32());
        Assert.True(demoPage.GetProperty("totalAmount").GetDecimal() > 0);
    }

    [Fact]
    public void SeedUserIds_AreTheSameInBothApis()
    {
        // Two databases, no foreign key: the demo login only sees its expenses if both seeds use the same ids.
        var identityIds = SeedUserIds("Identity");
        var expensesIds = SeedUserIds("Expenses");

        Assert.Equal([DemoId, OtherId], identityIds);
        Assert.Equal(identityIds, expensesIds);
    }

    [Fact]
    public async Task Seeding_IsOffUnlessEnabled()
    {
        // A fresh factory: the class fixture's database may already hold the seed from the test above.
        await using var fresh = new ExpensesApiFactory();
        using var client = fresh.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestJwt.For(DemoId));

        var page = await client.GetFromJsonAsync<JsonElement>("/api/expenses");

        Assert.Equal(0, page.GetProperty("totalCount").GetInt32());
    }

    private static Guid[] SeedUserIds(string context)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ExpenseTracker.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var path = Path.Combine(directory.FullName, "src", context, $"{context}.Api", "appsettings.json");
        using var settings = JsonDocument.Parse(File.ReadAllText(path));
        return settings.RootElement.GetProperty("Seed").GetProperty("Users").EnumerateArray()
            .Select(u => u.GetProperty("Id").GetGuid())
            .ToArray();
    }
}
