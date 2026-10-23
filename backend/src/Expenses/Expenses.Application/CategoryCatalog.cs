using Expenses.Application.Contracts;
using Expenses.Domain;

namespace Expenses.Application;

/// <summary>The fixed list behind GET /api/categories, in enum order. Built once; the set never changes at runtime.</summary>
public static class CategoryCatalog
{
    public static IReadOnlyList<CategoryResponse> All { get; } =
        Enum.GetValues<ExpenseCategory>()
            .Select(c => new CategoryResponse(c.ToString(), c.ToString()))
            .ToArray();
}
