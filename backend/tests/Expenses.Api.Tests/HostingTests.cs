using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Expenses.Application;
using Expenses.Domain;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Expenses.Api.Tests;

// Cross-cutting behavior of the host: error handling, CORS, configuration and Swagger (Milestone C).
public sealed class HostingTests : IClassFixture<ExpensesApiFactory>
{
    private const string Frontend = "http://localhost:4200";

    private readonly ExpensesApiFactory _factory;

    public HostingTests(ExpensesApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- Global exception handler ----------

    [Fact]
    public async Task UnhandledException_Returns500ProblemDetailsWithoutInternals()
    {
        using var failing = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<IExpenseRepository, ThrowingExpenseRepository>()));
        using var client = failing.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", TestJwt.For(Guid.NewGuid()));

        using var response = await client.GetAsync("/api/expenses");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("no such table", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SqliteException", raw, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", raw, StringComparison.Ordinal);
        Assert.Equal(500, JsonDocument.Parse(raw).RootElement.GetProperty("status").GetInt32());
    }

    // ---------- CORS ----------

    [Fact]
    public async Task Preflight_FromTheFrontend_IsAllowed()
    {
        using var client = _factory.CreateClient();

        using var response = await client.SendAsync(Preflight("/api/expenses", Frontend, "POST"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(Frontend, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        var allowedHeaders = string.Join(',', response.Headers.GetValues("Access-Control-Allow-Headers"));
        Assert.Contains("authorization", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preflight_FromAnotherOrigin_IsNotAllowed()
    {
        using var client = _factory.CreateClient();

        using var response = await client.SendAsync(Preflight("/api/expenses", "http://evil.example", "POST"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task UnauthorizedResponse_StillCarriesCorsHeaders()
    {
        // CORS runs before authentication, so the browser can read the 401 and send the user to the login page.
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/expenses");
        request.Headers.Add("Origin", Frontend);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(Frontend, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task CreatedResponse_ExposesLocationToTheFrontend()
    {
        using var client = _factory.CreateClientFor(Guid.NewGuid());
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/expenses")
        {
            Content = JsonContent.Create(new
            {
                description = "Lunch",
                amount = 10m,
                category = "Food",
                date = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            }),
        };
        request.Headers.Add("Origin", Frontend);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Location", string.Join(',', response.Headers.GetValues("Access-Control-Expose-Headers")), StringComparison.Ordinal);
    }

    // ---------- Configuration ----------

    [Fact]
    public void Startup_WithoutSigningKey_Fails()
    {
        using var misconfigured = _factory.WithWebHostBuilder(builder => builder.UseSetting("Jwt:SigningKey", string.Empty));

        var exception = Assert.ThrowsAny<Exception>(() => misconfigured.CreateClient());

        Assert.Contains("SigningKey", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SigningKey_IsOnlyInTheDevelopmentSettings()
    {
        var folder = SourceFolder("src", "Expenses", "Expenses.Api");

        using var production = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "appsettings.json")));
        using var development = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "appsettings.Development.json")));

        Assert.False(production.RootElement.GetProperty("Jwt").TryGetProperty("SigningKey", out _));
        var devKey = development.RootElement.GetProperty("Jwt").GetProperty("SigningKey").GetString();
        Assert.StartsWith("DEVELOPMENT-ONLY", devKey, StringComparison.Ordinal);
    }

    // ---------- Swagger ----------

    [Fact]
    public async Task Swagger_InDevelopment_OffersBearerAuthentication()
    {
        using var development = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = development.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var scheme = document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        Assert.True(document.RootElement.TryGetProperty("security", out _));
    }

    [Fact]
    public async Task Swagger_OutsideDevelopment_IsNotExposed()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---------- Helpers ----------

    private static HttpRequestMessage Preflight(string url, string origin, string method)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, url);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");
        return request;
    }

    private static string SourceFolder(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ExpenseTracker.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory.FullName, .. parts]);
    }

    private sealed class ThrowingExpenseRepository : IExpenseRepository
    {
        private static SqliteException Failure() => new("SQLite Error 1: 'no such table: Expenses'.", 1);

        public Task<Expense?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken) => throw Failure();

        public Task<ExpensePage> ListAsync(Guid userId, ExpenseListFilter filter, CancellationToken cancellationToken) =>
            throw Failure();

        public Task AddAsync(Expense expense, CancellationToken cancellationToken) => throw Failure();

        public Task<bool> UpdateAsync(Expense expense, CancellationToken cancellationToken) => throw Failure();

        public Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken) => throw Failure();
    }
}
