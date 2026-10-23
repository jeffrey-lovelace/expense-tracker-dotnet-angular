using Expenses.Infrastructure.Persistence;

using Microsoft.Data.Sqlite;

namespace Expenses.Infrastructure.Tests;

/// <summary>
/// A private in-memory database per test. A shared-cache memory database lives as long as one connection to it is
/// open, so this class keeps one open while the repository opens and disposes its own connections.
/// </summary>
internal sealed class SqliteTestDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _keepAlive;

    private SqliteTestDatabase(string connectionString)
    {
        ConnectionFactory = new SqliteConnectionFactory(connectionString);
        _keepAlive = ConnectionFactory.Create();
    }

    public ISqliteConnectionFactory ConnectionFactory { get; }

    public static async Task<SqliteTestDatabase> CreateAsync(bool migrate = true)
    {
        var database = new SqliteTestDatabase($"Data Source=expenses-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        await database._keepAlive.OpenAsync();
        if (migrate)
        {
            await new SqliteMigrator(database.ConnectionFactory).MigrateAsync(CancellationToken.None);
        }

        return database;
    }

    /// <summary>Runs a scalar query directly, to check what was really stored.</summary>
    public async Task<object?> ScalarAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = _keepAlive.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return await command.ExecuteScalarAsync();
    }

    public ValueTask DisposeAsync() => _keepAlive.DisposeAsync();
}
