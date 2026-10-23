using Microsoft.Data.Sqlite;

namespace Expenses.Infrastructure.Persistence;

// Registered as a singleton: it only holds the connection string, so sharing it is safe.
public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public SqliteConnection Create() => new(_connectionString);
}
