using System.Globalization;
using System.Reflection;

using Microsoft.Data.Sqlite;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Applies the embedded <c>Persistence/Migrations/NNNN_name.sql</c> scripts that are not yet recorded in
/// <c>SchemaVersions</c>, in order, each one in its own transaction.
/// </summary>
public sealed class SqliteMigrator
{
    private const string ResourceMarker = ".Persistence.Migrations.";

    private const string CreateVersionsTableSql = """
        CREATE TABLE IF NOT EXISTS SchemaVersions (
            Version      INTEGER NOT NULL PRIMARY KEY,
            Name         TEXT    NOT NULL,
            AppliedAtUtc TEXT    NOT NULL
        ) STRICT;
        """;

    private const string SelectAppliedSql = "SELECT Version FROM SchemaVersions;";

    private const string InsertVersionSql =
        "INSERT INTO SchemaVersions (Version, Name, AppliedAtUtc) VALUES (@version, @name, @appliedAtUtc);";

    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteMigrator(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <returns>The names of the scripts applied by this call (empty when the schema is up to date).</returns>
    public async Task<IReadOnlyList<string>> MigrateAsync(CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.Create();
        await connection.OpenAsync(cancellationToken);

        // Persistent per database file; must run outside a transaction. In-memory databases ignore it.
        await ExecuteAsync(connection, null, "PRAGMA journal_mode = WAL;", cancellationToken);
        await ExecuteAsync(connection, null, CreateVersionsTableSql, cancellationToken);

        var applied = await ReadAppliedVersionsAsync(connection, cancellationToken);
        var appliedNow = new List<string>();

        foreach (var script in LoadScripts().Where(s => !applied.Contains(s.Version)))
        {
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            await ExecuteAsync(connection, transaction, script.Sql, cancellationToken);

            await using (var insert = connection.CreateCommand())
            {
                insert.Transaction = transaction;
                insert.CommandText = InsertVersionSql;
                insert.Parameters.AddWithValue("@version", script.Version);
                insert.Parameters.AddWithValue("@name", script.Name);
                insert.Parameters.AddWithValue("@appliedAtUtc", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            appliedNow.Add(script.Name);
        }

        return appliedNow;
    }

    private static async Task<HashSet<long>> ReadAppliedVersionsAsync(
        SqliteConnection connection, CancellationToken cancellationToken)
    {
        var versions = new HashSet<long>();
        await using var command = connection.CreateCommand();
        command.CommandText = SelectAppliedSql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            versions.Add(reader.GetInt64(0));
        }

        return versions;
    }

    // Resource names look like "Identity.Infrastructure.Persistence.Migrations.0001_create_users.sql".
    private static List<MigrationScript> LoadScripts()
    {
        var assembly = typeof(SqliteMigrator).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(n => n.Contains(ResourceMarker, StringComparison.Ordinal)
                && n.EndsWith(".sql", StringComparison.Ordinal))
            .Select(n => ReadScript(assembly, n))
            .OrderBy(s => s.Version)
            .ToList();
    }

    private static MigrationScript ReadScript(Assembly assembly, string resourceName)
    {
        var fileName = resourceName[(resourceName.IndexOf(ResourceMarker, StringComparison.Ordinal) + ResourceMarker.Length)..];
        var name = Path.GetFileNameWithoutExtension(fileName);
        var version = long.Parse(name[..name.IndexOf('_', StringComparison.Ordinal)], CultureInfo.InvariantCulture);

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Migration resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return new MigrationScript(version, name, reader.ReadToEnd());
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection, SqliteTransaction? transaction, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record MigrationScript(long Version, string Name, string Sql);
}
