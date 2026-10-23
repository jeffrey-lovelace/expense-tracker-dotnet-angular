using System.Net.Http.Headers;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace Expenses.Api.Tests;

/// <summary>
/// Runs the real Expenses.Api in memory against a private in-memory SQLite database and a test signing key.
/// One factory (and one database) per test class.
/// </summary>
public sealed class ExpensesApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString =
        $"Data Source=expenses-api-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    private readonly SqliteConnection _keepAlive;

    public ExpensesApiFactory()
    {
        // The shared-cache memory database exists only while a connection to it is open.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    public HttpClient CreateClientFor(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestJwt.For(userId));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Jwt:Issuer", TestJwt.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwt.Audience);
        builder.UseSetting("Jwt:SigningKey", TestJwt.SigningKey);
        builder.UseSetting("Seed:Enabled", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
