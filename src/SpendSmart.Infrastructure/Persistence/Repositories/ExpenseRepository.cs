using Microsoft.EntityFrameworkCore;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Common;
using SpendSmart.Application.Expenses;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseRepository(SpendSmartDbContext db) : IExpenseRepository
{
    public Task<Expense?> GetAsync(int id, string userId, CancellationToken cancellationToken = default) =>
        db.Expenses
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, cancellationToken);

    public async Task<(IReadOnlyList<Expense> Items, int TotalCount)> ListAsync(
        string userId, ExpenseQuery query, CancellationToken cancellationToken = default)
    {
        var expenses = db.Expenses.AsNoTracking().Where(e => e.UserId == userId);

        if (query.CategoryId is { } categoryId)
        {
            expenses = expenses.Where(e => e.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var categoryName = query.Category.Trim().ToLower();
            expenses = expenses.Where(e => e.Category!.Name.ToLower() == categoryName);
        }

        if (query.From is { } from)
        {
            expenses = expenses.Where(e => e.Date >= from);
        }

        if (query.To is { } to)
        {
            expenses = expenses.Where(e => e.Date <= to);
        }

        if (query.MinAmount is { } min)
        {
            expenses = expenses.Where(e => e.Amount >= min);
        }

        if (query.MaxAmount is { } max)
        {
            expenses = expenses.Where(e => e.Amount <= max);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            expenses = expenses.Where(e => EF.Functions.ILike(e.Description, pattern));
        }

        var totalCount = await expenses.CountAsync(cancellationToken);

        var items = await Sort(expenses, query)
            .Include(e => e.Category)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<CategoryTotal>> GetCategoryTotalsAsync(
        string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await db.Expenses
            .Where(e => e.UserId == userId && e.Date >= from && e.Date <= to)
            .Join(db.Categories, e => e.CategoryId, c => c.Id, (e, c) => new { e.Amount, c.Id, c.Name, c.Color })
            .GroupBy(x => new { x.Id, x.Name, x.Color }, x => x.Amount)
            .Select(g => new CategoryTotal(g.Key.Id, g.Key.Name, g.Key.Color, g.Sum()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyTotal>> GetMonthlyTotalsAsync(
        string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return await db.Expenses
            .Where(e => e.UserId == userId && e.Date >= from && e.Date <= to)
            .GroupBy(e => new { e.Date.Year, e.Date.Month }, e => e.Amount)
            .Select(g => new MonthlyTotal(g.Key.Year, g.Key.Month, g.Sum()))
            .ToListAsync(cancellationToken);
    }

    public void Add(Expense expense) => db.Expenses.Add(expense);

    public void Remove(Expense expense) => db.Expenses.Remove(expense);

    private static IQueryable<Expense> Sort(IQueryable<Expense> expenses, ExpenseQuery query)
    {
        var ascending = query.SortDirection == SortDirection.Asc;
        var sorted = query.SortBy switch
        {
            ExpenseSortBy.Amount => ascending ? expenses.OrderBy(e => e.Amount) : expenses.OrderByDescending(e => e.Amount),
            ExpenseSortBy.Description => ascending ? expenses.OrderBy(e => e.Description) : expenses.OrderByDescending(e => e.Description),
            _ => ascending ? expenses.OrderBy(e => e.Date) : expenses.OrderByDescending(e => e.Date),
        };

        // Tie-breaker keeps paging stable when many rows share the same sort value.
        return ascending ? sorted.ThenBy(e => e.Id) : sorted.ThenByDescending(e => e.Id);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace(@"\", @"\\", StringComparison.Ordinal)
             .Replace("%", @"\%", StringComparison.Ordinal)
             .Replace("_", @"\_", StringComparison.Ordinal);
}
