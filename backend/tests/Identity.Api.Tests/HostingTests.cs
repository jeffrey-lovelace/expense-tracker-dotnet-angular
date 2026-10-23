using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Identity.Application;
using Identity.Domain;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Api.Tests;

// Cross-cutting behavior of the host: error handling, CORS, configuration, Swagger and logging (Milestone C).
public sealed class HostingTests : IClassFixture<IdentityApiFactory>
{
    private const string Frontend = "http://localhost:4200";
    private const string Password = "Demo123!";

    private readonly IdentityApiFactory _factory;

    public HostingTests(IdentityApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- Global exception handler ----------

    [Fact]
    public async Task UnhandledException_Returns500ProblemDetailsWithoutInternals()
    {
        using var failing = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<IUserRepository, ThrowingUserRepository>()));
        using var client = failing.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { email = "demo@ballastlane.com", password = Password });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("no such table", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SqliteException", raw, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", raw, StringComparison.Ordinal);
    }

    // ---------- Logging ----------

    [Fact]
    public async Task Logs_NeverContainPasswordsOrTokens()
    {
        var logs = new CapturingLoggerProvider();
        using var logged = _factory.WithWebHostBuilder(builder => builder
            .UseSetting("Logging:LogLevel:Default", "Trace")
            .UseSetting("Logging:LogLevel:Microsoft.AspNetCore", "Trace")
            .ConfigureTestServices(services => services.AddSingleton<ILoggerProvider>(logs)));
        using var client = logged.CreateClient();
        var email = $"logs-{Guid.NewGuid():N}@example.com";

        using var register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, displayName = "Log Test", password = Password });
        var token = (await register.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
        (await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Wrong123!" })).Dispose();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        (await client.GetAsync("/api/auth/me")).Dispose();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token + "tampered");
        (await client.GetAsync("/api/auth/me")).Dispose();

        Assert.NotEmpty(logs.Messages);
        Assert.DoesNotContain(logs.Messages, m => m.Contains(Password, StringComparison.Ordinal));
        Assert.DoesNotContain(logs.Messages, m => m.Contains("Wrong123!", StringComparison.Ordinal));
        Assert.DoesNotContain(logs.Messages, m => m.Contains(token, StringComparison.Ordinal));
    }

    // ---------- CORS ----------

    [Fact]
    public async Task Preflight_FromTheFrontend_IsAllowed()
    {
        using var client = _factory.CreateClient();

        using var response = await client.SendAsync(Preflight("/api/auth/login", Frontend));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(Frontend, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Preflight_FromAnotherOrigin_IsNotAllowed()
    {
        using var client = _factory.CreateClient();

        using var response = await client.SendAsync(Preflight("/api/auth/login", "http://evil.example"));

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
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
        var folder = SourceFolder("src", "Identity", "Identity.Api");

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
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
    }

    // ---------- Helpers ----------

    private static HttpRequestMessage Preflight(string url, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, url);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
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

    private sealed class ThrowingUserRepository : IUserRepository
    {
        private static SqliteException Failure() => new("SQLite Error 1: 'no such table: Users'.", 1);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw Failure();

        public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => throw Failure();

        public Task<bool> AddAsync(User user, CancellationToken cancellationToken) => throw Failure();
    }

    /// <summary>Collects every formatted log message, including exception text, from every category.</summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _messages = new();

        public IReadOnlyCollection<string> Messages => _messages;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                messages.Enqueue(formatter(state, exception) + " " + exception);
            }
        }
    }
}
