using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.Expenses;

public sealed record ExpenseDto(
    int Id,
    decimal Amount,
    string Description,
    DateOnly Date,
    int CategoryId,
    string CategoryName,
    string CategoryColor,
    bool IsRecurring);

/// <summary>Used for both create and update.</summary>
public sealed record SaveExpenseRequest(decimal Amount, string Description, DateOnly Date, int CategoryId);

public enum ExpenseSortBy
{
    Date = 0,
    Amount = 1,
    Description = 2,
}

public enum SortDirection
{
    Desc = 0,
    Asc = 1,
}

/// <summary>Filtering, sorting and paging for the expense list. Every filter is optional.</summary>
public sealed record ExpenseQuery
{
    public const int MaxPageSize = 100;

    /// <summary>Filter by category id.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Filter by category name (case-insensitive), e.g. <c>?category=groceries</c>.</summary>
    public string? Category { get; init; }

    /// <summary>Inclusive start date (yyyy-MM-dd).</summary>
    public DateOnly? From { get; init; }

    /// <summary>Inclusive end date (yyyy-MM-dd).</summary>
    public DateOnly? To { get; init; }

    public decimal? MinAmount { get; init; }

    public decimal? MaxAmount { get; init; }

    /// <summary>Text contained in the description.</summary>
    public string? Search { get; init; }

    public ExpenseSortBy SortBy { get; init; } = ExpenseSortBy.Date;

    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

internal static class ExpenseMappings
{
    public static ExpenseDto ToDto(this Expense expense, Category category) => new(
        expense.Id,
        expense.Amount,
        expense.Description,
        expense.Date,
        category.Id,
        category.Name,
        category.Color,
        expense.RecurringExpenseId is not null);

    public static ExpenseDto ToDto(this Expense expense) =>
        expense.ToDto(expense.Category
            ?? throw new InvalidOperationException("Expense.Category must be loaded before mapping."));
}
