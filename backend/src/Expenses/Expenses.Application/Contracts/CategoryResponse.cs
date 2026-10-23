namespace Expenses.Application.Contracts;

// Value is what requests send back; Label is what the UI shows.
public sealed record CategoryResponse(string Value, string Label);
