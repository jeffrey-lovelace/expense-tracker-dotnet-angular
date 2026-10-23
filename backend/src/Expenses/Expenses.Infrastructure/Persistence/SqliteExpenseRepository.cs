using System.Globalization;

using Expenses.Application;
using Expenses.Domain;

using Microsoft.Data.Sqlite;

namespace Expenses.Infrastructure.Persistence;

/// <summary>
/// Adapter from ADO.NET to <see cref="IExpenseRepository"/>. The only class that knows the Expenses SQL.
/// Every statement is a constant with parameters, and every statement filters by UserId.
/// Amounts are stored as INTEGER cents (decimal 12.50 is 1250), dates as yyyy-MM-dd text, instants as ISO 8601 UTC.
/// </summary>
public sealed class SqliteExpenseRepository : IExpenseRepository
{
    private const string Columns =
        "Id, UserId, Description, AmountCents, Category, ExpenseDate, CreatedAtUtc, UpdatedAtUtc";

    private const string SelectByIdSql =
        "SELECT " + Columns + " FROM Expenses WHERE Id = @id AND UserId = @userId;";

    // One static statement for every filter combination: a NULL parameter disables its condition (see docs/database.md).
    private const string FilterSql = """
        WHERE UserId = @userId
          AND (@category IS NULL OR Category = @category)
          AND (@from IS NULL OR ExpenseDate >= @from)
          AND (@to IS NULL OR ExpenseDate <= @to)
        """;

    private const string SelectPageSql =
        "SELECT " + Columns + " FROM Expenses " + FilterSql +
        " ORDER BY ExpenseDate DESC, CreatedAtUtc DESC, Id LIMIT @limit OFFSET @offset;";

    private const string SelectTotalsSql =
        "SELECT COUNT(*), COALESCE(SUM(AmountCents), 0) FROM Expenses " + FilterSql + ";";

    private const string InsertSql = """
        INSERT INTO Expenses (Id, UserId, Description, AmountCents, Category, ExpenseDate, CreatedAtUtc, UpdatedAtUtc)
        VALUES (@id, @userId, @description, @amountCents, @category, @date, @createdAtUtc, @updatedAtUtc);
        """;

    private const string UpdateSql = """
        UPDATE Expenses
        SET Description = @description, AmountCents = @amountCents, Category = @category,
            ExpenseDate = @date, UpdatedAtUtc = @updatedAtUtc
        WHERE Id = @id AND UserId = @userId;
        """;

    private const string DeleteSql = "DELETE FROM Expenses WHERE Id = @id AND UserId = @userId;";

    private const string DateFormat = "yyyy-MM-dd";

    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteExpenseRepository(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Expense?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectByIdSql;
        command.Parameters.AddWithValue("@id", ToText(id));
        command.Parameters.AddWithValue("@userId", ToText(userId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? new ExpenseReader(reader).Read() : null;
    }

    public async Task<ExpensePage> ListAsync(Guid userId, ExpenseListFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await using var connection = await OpenAsync(cancellationToken);

        int totalCount;
        long totalCents;
        await using (var totals = connection.CreateCommand())
        {
            totals.CommandText = SelectTotalsSql;
            AddFilterParameters(totals, userId, filter);
            await using var reader = await totals.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            totalCount = reader.GetInt32(0);
            totalCents = reader.GetInt64(1);
        }

        var items = new List<Expense>(filter.PageSize);
        await using (var page = connection.CreateCommand())
        {
            page.CommandText = SelectPageSql;
            AddFilterParameters(page, userId, filter);
            page.Parameters.AddWithValue("@limit", filter.PageSize);
            page.Parameters.AddWithValue("@offset", (long)(filter.Page - 1) * filter.PageSize);
            await using var reader = await page.ExecuteReaderAsync(cancellationToken);
            var expenseReader = new ExpenseReader(reader);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(expenseReader.Read());
            }
        }

        return new ExpensePage(items, totalCount, FromCents(totalCents));
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expense);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertSql;
        AddExpenseParameters(command, expense);
        command.Parameters.AddWithValue("@createdAtUtc", ToText(expense.CreatedAtUtc));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Expense expense, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expense);
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = UpdateSql;
        AddExpenseParameters(command, expense);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        command.Parameters.AddWithValue("@id", ToText(id));
        command.Parameters.AddWithValue("@userId", ToText(userId));
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
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

    private static void AddExpenseParameters(SqliteCommand command, Expense expense)
    {
        command.Parameters.AddWithValue("@id", ToText(expense.Id));
        command.Parameters.AddWithValue("@userId", ToText(expense.UserId));
        command.Parameters.AddWithValue("@description", expense.Description);
        command.Parameters.AddWithValue("@amountCents", ToCents(expense.Amount));
        command.Parameters.AddWithValue("@category", expense.Category.ToString());
        command.Parameters.AddWithValue("@date", expense.Date.ToString(DateFormat, CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("@updatedAtUtc", ToText(expense.UpdatedAtUtc));
    }

    private static void AddFilterParameters(SqliteCommand command, Guid userId, ExpenseListFilter filter)
    {
        command.Parameters.AddWithValue("@userId", ToText(userId));
        command.Parameters.AddWithValue("@category", (object?)filter.Category?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@from", (object?)filter.From?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "@to", (object?)filter.To?.ToString(DateFormat, CultureInfo.InvariantCulture) ?? DBNull.Value);
    }

    private static string ToText(Guid id) => id.ToString("D");

    private static string ToText(DateTime utc) => utc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    // The domain guarantees at most two decimals, so the multiplication is exact. checked() turns a bug into an error.
    private static long ToCents(decimal amount) => checked((long)(amount * 100m));

    private static decimal FromCents(long cents) => cents / 100m;

    /// <summary>Caches the column ordinals once per reader, not once per row.</summary>
    private sealed class ExpenseReader
    {
        private readonly SqliteDataReader _reader;
        private readonly int _id;
        private readonly int _userId;
        private readonly int _description;
        private readonly int _amountCents;
        private readonly int _category;
        private readonly int _date;
        private readonly int _createdAtUtc;
        private readonly int _updatedAtUtc;

        public ExpenseReader(SqliteDataReader reader)
        {
            _reader = reader;
            _id = reader.GetOrdinal("Id");
            _userId = reader.GetOrdinal("UserId");
            _description = reader.GetOrdinal("Description");
            _amountCents = reader.GetOrdinal("AmountCents");
            _category = reader.GetOrdinal("Category");
            _date = reader.GetOrdinal("ExpenseDate");
            _createdAtUtc = reader.GetOrdinal("CreatedAtUtc");
            _updatedAtUtc = reader.GetOrdinal("UpdatedAtUtc");
        }

        public Expense Read() =>
            Expense.Rehydrate(
                Guid.Parse(_reader.GetString(_id)),
                Guid.Parse(_reader.GetString(_userId)),
                _reader.GetString(_description),
                FromCents(_reader.GetInt64(_amountCents)),
                Enum.Parse<ExpenseCategory>(_reader.GetString(_category)),
                DateOnly.ParseExact(_reader.GetString(_date), DateFormat, CultureInfo.InvariantCulture),
                ParseUtc(_reader.GetString(_createdAtUtc)),
                ParseUtc(_reader.GetString(_updatedAtUtc)));

        private static DateTime ParseUtc(string value) =>
            DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
