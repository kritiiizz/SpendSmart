using SpendSmart.Application.Common;
using SpendSmart.Application.Expenses;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.Abstractions;

// Every read takes the owner's id explicitly, so a query can't accidentally return another user's data.

public interface IExpenseRepository
{
    Task<Expense?> GetAsync(int id, string userId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Expense> Items, int TotalCount)> ListAsync(
        string userId, ExpenseQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryTotal>> GetCategoryTotalsAsync(
        string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyTotal>> GetMonthlyTotalsAsync(
        string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    void Add(Expense expense);

    void Remove(Expense expense);
}

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(string userId, CancellationToken cancellationToken = default);

    Task<Category?> GetAsync(int id, string userId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string userId, string name, int? excludingId = null, CancellationToken cancellationToken = default);

    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);

    void Add(Category category);

    void Remove(Category category);
}

public interface IBudgetRepository
{
    Task<IReadOnlyList<Budget>> ListAsync(string userId, CancellationToken cancellationToken = default);

    Task<Budget?> GetAsync(int id, string userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForCategoryAsync(string userId, int categoryId, CancellationToken cancellationToken = default);

    void Add(Budget budget);

    void Remove(Budget budget);
}

public interface IRecurringExpenseRepository
{
    Task<IReadOnlyList<RecurringExpense>> ListAsync(string userId, CancellationToken cancellationToken = default);

    Task<RecurringExpense?> GetAsync(int id, string userId, CancellationToken cancellationToken = default);

    /// <summary>Active templates for all users whose next occurrence is on or before <paramref name="today"/>.</summary>
    Task<IReadOnlyList<RecurringExpense>> GetDueAsync(DateOnly today, CancellationToken cancellationToken = default);

    void Add(RecurringExpense recurringExpense);

    void Remove(RecurringExpense recurringExpense);
}
