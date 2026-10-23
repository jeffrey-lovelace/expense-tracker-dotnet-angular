using System.Globalization;

using Identity.Application;
using Identity.Domain;

using Microsoft.Data.Sqlite;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// Adapter from ADO.NET to <see cref="IUserRepository"/>. The only class that knows the Users SQL.
/// Every statement is a constant with parameters. Email uniqueness is enforced by the case-insensitive unique index.
/// </summary>
public sealed class SqliteUserRepository : IUserRepository
{
    private const string Columns = "Id, Email, DisplayName, PasswordHash, CreatedAtUtc";

    private const string SelectByIdSql = "SELECT " + Columns + " FROM Users WHERE Id = @id;";

    private const string SelectByEmailSql = "SELECT " + Columns + " FROM Users WHERE Email = @email;";

    private const string InsertSql = """
        INSERT INTO Users (Id, Email, DisplayName, PasswordHash, CreatedAtUtc)
        VALUES (@id, @email, @displayName, @passwordHash, @createdAtUtc);
        """;

    // SQLITE_CONSTRAINT_UNIQUE: https://www.sqlite.org/rescode.html#constraint_unique
    private const int UniqueConstraintFailed = 2067;

    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteUserRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetSingleAsync(SelectByIdSql, "@id", id.ToString("D"), cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        GetSingleAsync(SelectByEmailSql, "@email", normalizedEmail, cancellationToken);

    public async Task<bool> AddAsync(User user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertSql;
        command.Parameters.AddWithValue("@id", user.Id.ToString("D"));
        command.Parameters.AddWithValue("@email", user.Email);
        command.Parameters.AddWithValue("@displayName", user.DisplayName);
        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@createdAtUtc", user.CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            return true;
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == UniqueConstraintFailed)
        {
            // The only unique index besides the primary key is UX_Users_Email.
            return false;
        }
    }

    private async Task<User?> GetSingleAsync(
        string sql, string parameterName, string value, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue(parameterName, value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return User.Rehydrate(
            Guid.Parse(reader.GetString(reader.GetOrdinal("Id"))),
            reader.GetString(reader.GetOrdinal("Email")),
            reader.GetString(reader.GetOrdinal("DisplayName")),
            reader.GetString(reader.GetOrdinal("PasswordHash")),
            DateTime.Parse(
                reader.GetString(reader.GetOrdinal("CreatedAtUtc")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = _connectionFactory.Create();
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
