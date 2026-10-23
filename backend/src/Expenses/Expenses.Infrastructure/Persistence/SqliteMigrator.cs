namespace Expenses.Infrastructure.Persistence;

/// <summary>
/// Applies the embedded <c>Persistence/Migrations/NNNN_name.sql</c> scripts that are not yet recorded in
/// <c>SchemaVersions</c>, in order, each one in its own transaction.
/// </summary>
public sealed class SqliteMigrator
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteMigrator(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <returns>The names of the scripts applied by this call (empty when the schema is up to date).</returns>
    public Task<IReadOnlyList<string>> MigrateAsync(CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
