using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Expenses.Api.Tests;

public sealed class ExpensesEndpointsTests : IClassFixture<ExpensesApiFactory>
{
    private static readonly Guid OwnerId = Guid.Parse("6f1c3a52-8d4e-4b7a-9c21-0e5f7d8a9b10");
    private static readonly Guid OtherUserId = Guid.Parse("a2d94e17-3b6c-4f08-8e5a-71c9b0d3f264");
    private static readonly string Yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private readonly ExpensesApiFactory _factory;

    public ExpensesEndpointsTests(ExpensesApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- Authentication ----------

    [Theory]
    [InlineData("GET", "/api/expenses")]
    [InlineData("GET", "/api/expenses/6f1c3a52-8d4e-4b7a-9c21-0e5f7d8a9b10")]
    [InlineData("POST", "/api/expenses")]
    [InlineData("PUT", "/api/expenses/6f1c3a52-8d4e-4b7a-9c21-0e5f7d8a9b10")]
    [InlineData("DELETE", "/api/expenses/6f1c3a52-8d4e-4b7a-9c21-0e5f7d8a9b10")]
    public async Task ExpenseEndpoints_WithoutToken_Return401(string method, string url)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(ValidBody());
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, string> BrokenTokens => new()
    {
        { "expired", TestJwt.For(OwnerId, expiresUtc: DateTime.UtcNow.AddMinutes(-5)) },
        { "wrong signature", TestJwt.For(OwnerId, signingKey: "another-key-that-is-long-enough-0123456789") },
        { "wrong issuer", TestJwt.For(OwnerId, issuer: "someone-else") },
        { "wrong audience", TestJwt.For(OwnerId, audience: "another-api") },
        { "not a jwt", "not-a-token" },
    };

    [Theory]
    [MemberData(nameof(BrokenTokens))]
    public async Task GetExpenses_WithInvalidToken_Returns401(string reason, string token)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync("/api/expenses");

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, reason);
    }

    [Fact]
    public async Task GetExpenses_WithTokenWithoutSub_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.For(OwnerId, includeSub: false));

        using var response = await client.GetAsync("/api/expenses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Create ----------

    [Fact]
    public async Task PostExpense_WithValidBody_Returns201WithLocationAndBody()
    {
        using var client = _factory.CreateClientFor(OwnerId);

        using var response = await client.PostAsJsonAsync("/api/expenses", ValidBody("Lunch", 12.50m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal($"/api/expenses/{id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal("Lunch", body.GetProperty("description").GetString());
        Assert.Equal(12.50m, body.GetProperty("amount").GetDecimal());
        Assert.Equal("Food", body.GetProperty("category").GetString());
        Assert.Equal(Yesterday, body.GetProperty("date").GetString());
        Assert.False(body.TryGetProperty("userId", out _));

        using var fetched = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }

    [Fact]
    public async Task PostExpense_WithUserIdInBody_IgnoresItAndUsesTokenOwner()
    {
        using var owner = _factory.CreateClientFor(OwnerId);
        using var other = _factory.CreateClientFor(OtherUserId);
        var body = new { description = "Sneaky", amount = 5m, category = "Food", date = Yesterday, userId = OtherUserId };

        using var created = await owner.PostAsJsonAsync("/api/expenses", body);
        var id = (await ReadJsonAsync(created)).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/expenses/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/expenses/{id}")).StatusCode);
    }

    [Fact]
    public async Task PostExpense_WithInvalidFields_Returns400WithEveryFieldError()
    {
        using var client = _factory.CreateClientFor(OwnerId);
        var body = new { description = "x", amount = 0m, category = "Groceries", date = "2999-01-01" };

        using var response = await client.PostAsJsonAsync("/api/expenses", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("description", out _));
        Assert.True(errors.TryGetProperty("amount", out _));
        Assert.True(errors.TryGetProperty("category", out _));
        Assert.True(errors.TryGetProperty("date", out _));
    }

    [Fact]
    public async Task PostExpense_WithMissingField_Returns400KeyedByJsonName()
    {
        using var client = _factory.CreateClientFor(OwnerId);
        var body = new { amount = 10m, category = "Food", date = Yesterday };

        using var response = await client.PostAsJsonAsync("/api/expenses", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ReadJsonAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("description", out _), errors.ToString());
    }

    // ---------- Read ----------

    [Fact]
    public async Task GetExpense_WithUnknownId_Returns404ProblemDetails()
    {
        using var client = _factory.CreateClientFor(OwnerId);

        using var response = await client.GetAsync($"/api/expenses/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task OtherUsersExpense_GetPutDelete_Return404AndLeaveItUntouched()
    {
        using var owner = _factory.CreateClientFor(OwnerId);
        using var other = _factory.CreateClientFor(OtherUserId);
        var id = await CreateAsync(owner, "Private", 9m);

        using var get = await other.GetAsync($"/api/expenses/{id}");
        using var put = await other.PutAsJsonAsync($"/api/expenses/{id}", ValidBody("Hacked", 1m));
        using var delete = await other.DeleteAsync($"/api/expenses/{id}");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        var stillThere = await ReadJsonAsync(await owner.GetAsync($"/api/expenses/{id}"));
        Assert.Equal("Private", stillThere.GetProperty("description").GetString());
    }

    [Fact]
    public async Task GetExpenses_ReturnsOnlyCallersExpensesWithTotals()
    {
        var userId = Guid.NewGuid(); // a fresh user, so other tests in this class do not change the totals
        using var client = _factory.CreateClientFor(userId);
        using var other = _factory.CreateClientFor(OtherUserId);
        await CreateAsync(client, "Bus", 2.50m);
        await CreateAsync(client, "Taxi", 10m);
        await CreateAsync(other, "Not mine", 99m);

        using var response = await client.GetAsync("/api/expenses?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(12.50m, body.GetProperty("totalAmount").GetDecimal());
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(20, body.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task GetExpenses_WithoutQuery_UsesDefaultPaging()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());

        var body = await ReadJsonAsync(await client.GetAsync("/api/expenses"));

        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(20, body.GetProperty("pageSize").GetInt32());
    }

    [Theory]
    [InlineData("?pageSize=500", "pageSize")]
    [InlineData("?page=0", "page")]
    [InlineData("?category=food", "category")]
    [InlineData("?from=2026-10-02&to=2026-10-01", "to")]
    public async Task GetExpenses_WithInvalidQuery_Returns400(string query, string field)
    {
        using var client = _factory.CreateClientFor(OwnerId);

        using var response = await client.GetAsync($"/api/expenses{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty(field, out _));
    }

    // ---------- Update ----------

    [Fact]
    public async Task PutExpense_WithValidBody_Returns200WithChanges()
    {
        using var client = _factory.CreateClientFor(OwnerId);
        var id = await CreateAsync(client, "Coffee", 3m);

        using var response = await client.PutAsJsonAsync($"/api/expenses/{id}", ValidBody("Dinner", 40m, "Entertainment"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal("Dinner", body.GetProperty("description").GetString());
        Assert.Equal(40m, body.GetProperty("amount").GetDecimal());
        Assert.Equal("Entertainment", body.GetProperty("category").GetString());
    }

    [Fact]
    public async Task PutExpense_WithInvalidBody_Returns400()
    {
        using var client = _factory.CreateClientFor(OwnerId);
        var id = await CreateAsync(client, "Coffee", 3m);

        using var response = await client.PutAsJsonAsync($"/api/expenses/{id}", ValidBody("Coffee", -1m));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("amount", out _));
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteExpense_WithOwnExpense_Returns204AndThenGetReturns404()
    {
        using var client = _factory.CreateClientFor(OwnerId);
        var id = await CreateAsync(client, "Coffee", 3m);

        using var response = await client.DeleteAsync($"/api/expenses/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/expenses/{id}")).StatusCode);
    }

    // ---------- Categories ----------

    [Fact]
    public async Task GetCategories_WithoutToken_Returns200WithEveryCategory()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = (await ReadJsonAsync(response)).EnumerateArray()
            .Select(c => c.GetProperty("value").GetString())
            .ToArray();
        Assert.Equal(
            ["Food", "Transport", "Housing", "Utilities", "Health", "Entertainment", "Shopping", "Education", "Other"],
            categories);
    }

    // ---------- Helpers ----------

    private static object ValidBody(string description = "Groceries", decimal amount = 10m, string category = "Food") =>
        new { description, amount, category, date = Yesterday };

    private static async Task<Guid> CreateAsync(HttpClient client, string description, decimal amount)
    {
        using var response = await client.PostAsJsonAsync("/api/expenses", ValidBody(description, amount));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.Clone();
    }
}
