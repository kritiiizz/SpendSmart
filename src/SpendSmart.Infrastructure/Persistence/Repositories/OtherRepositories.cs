using Microsoft.EntityFrameworkCore;
using SpendSmart.Application.Abstractions;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(SpendSmartDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(string userId, CancellationToken cancellationToken = default) =>
        await db.Categories.AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<Category?> GetAsync(int id, string userId, CancellationToken cancellationToken = default) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

    public Task<bool> NameExistsAsync(string userId, string name, int? excludingId = null, CancellationToken cancellationToken = default)
    {
        var lowered = name.ToLower();
        return db.Categories.AnyAsync(
            c => c.UserId == userId && c.Name.ToLower() == lowered && (excludingId == null || c.Id != excludingId),
            cancellationToken);
    }

    public async Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default) =>
        await db.Expenses.AnyAsync(e => e.CategoryId == id, cancellationToken)
        || await db.Budgets.AnyAsync(b => b.CategoryId == id, cancellationToken)
        || await db.RecurringExpenses.AnyAsync(r => r.CategoryId == id, cancellationToken);

    public void Add(Category category) => db.Categories.Add(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}

internal sealed class BudgetRepository(SpendSmartDbContext db) : IBudgetRepository
{
    public async Task<IReadOnlyList<Budget>> ListAsync(string userId, CancellationToken cancellationToken = default) =>
        await db.Budgets.AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId)
            .ToListAsync(cancellationToken);

    public Task<Budget?> GetAsync(int id, string userId, CancellationToken cancellationToken = default) =>
        db.Budgets.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);

    public Task<bool> ExistsForCategoryAsync(string userId, int categoryId, CancellationToken cancellationToken = default) =>
        db.Budgets.AnyAsync(b => b.UserId == userId && b.CategoryId == categoryId, cancellationToken);

    public void Add(Budget budget) => db.Budgets.Add(budget);

    public void Remove(Budget budget) => db.Budgets.Remove(budget);
}

internal sealed class RecurringExpenseRepository(SpendSmartDbContext db) : IRecurringExpenseRepository
{
    public async Task<IReadOnlyList<RecurringExpense>> ListAsync(string userId, CancellationToken cancellationToken = default) =>
        await db.RecurringExpenses.AsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.NextOccurrence)
            .ToListAsync(cancellationToken);

    public Task<RecurringExpense?> GetAsync(int id, string userId, CancellationToken cancellationToken = default) =>
        db.RecurringExpenses.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<RecurringExpense>> GetDueAsync(DateOnly today, CancellationToken cancellationToken = default) =>
        await db.RecurringExpenses
            .Where(r => r.IsActive && r.NextOccurrence <= today)
            .ToListAsync(cancellationToken);

    public void Add(RecurringExpense recurringExpense) => db.RecurringExpenses.Add(recurringExpense);

    public void Remove(RecurringExpense recurringExpense) => db.RecurringExpenses.Remove(recurringExpense);
}
