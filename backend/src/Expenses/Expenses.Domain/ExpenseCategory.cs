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
