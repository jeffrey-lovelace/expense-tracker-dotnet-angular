namespace Expenses.Domain;

// Must match the CHECK constraint in 0001_create_expenses.sql. Stored by name, never by number.
public enum ExpenseCategory
{
    Food,
    Transport,
    Housing,
    Utilities,
    Health,
    Entertainment,
    Shopping,
    Education,
    Other,
}

public static class ExpenseCategories
{
    private static readonly string[] Names = Enum.GetNames<ExpenseCategory>();

    /// <summary>
    /// Accepts exact category names only. <see cref="Enum.TryParse{TEnum}(string, out TEnum)"/> would also accept
    /// "5", " Food" or "Food,Transport".
    /// </summary>
    public static bool TryParse(string? value, out ExpenseCategory category)
    {
        if (value is not null && Names.Contains(value, StringComparer.Ordinal))
        {
            category = Enum.Parse<ExpenseCategory>(value);
            return true;
        }

        category = default;
        return false;
    }
}
