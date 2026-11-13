using FluentValidation;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.Budgets;

public interface IBudgetService
{
    /// <summary>All budgets with spending for the given month (defaults to the current month).</summary>
    Task<IReadOnlyList<BudgetDto>> ListAsync(int? year = null, int? month = null, CancellationToken cancellationToken = default);

    Task<BudgetDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<BudgetDto> CreateAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default);

    Task<BudgetDto> UpdateAsync(int id, UpdateBudgetRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class BudgetService(
    IBudgetRepository budgets,
    ICategoryRepository categories,
    IExpenseRepository expenses,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDashboardCache dashboardCache,
    TimeProvider timeProvider,
    IValidator<CreateBudgetRequest> createValidator,
    IValidator<UpdateBudgetRequest> updateValidator) : IBudgetService
{
    public async Task<IReadOnlyList<BudgetDto>> ListAsync(int? year = null, int? month = null, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        var list = await budgets.ListAsync(userId, cancellationToken);
        if (list.Count == 0)
        {
            return [];
        }

        var spending = await GetSpendingByCategoryAsync(userId, year, month, cancellationToken);
        return list
            .Select(b => ToDto(b, b.Category!, spending.GetValueOrDefault(b.CategoryId)))
            .OrderByDescending(b => b.PercentUsed)
            .ToList();
    }

    public async Task<BudgetDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var budget = await FindOwnedAsync(id, cancellationToken);
        return await ToDtoWithCurrentSpendingAsync(budget, budget.Category!, cancellationToken);
    }

    public async Task<BudgetDto> CreateAsync(CreateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var userId = currentUser.UserId;

        var category = await categories.GetAsync(request.CategoryId, userId, cancellationToken)
            ?? throw nameof(CreateBudgetRequest.CategoryId).ToValidationException("Category does not exist.");

        if (await budgets.ExistsForCategoryAsync(userId, category.Id, cancellationToken))
        {
            throw new ConflictException($"There is already a budget for '{category.Name}'. Edit that one instead.");
        }

        var budget = new Budget(userId, category.Id, request.MonthlyLimit);
        budgets.Add(budget);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        dashboardCache.Invalidate(userId);

        return await ToDtoWithCurrentSpendingAsync(budget, category, cancellationToken);
    }

    public async Task<BudgetDto> UpdateAsync(int id, UpdateBudgetRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var budget = await FindOwnedAsync(id, cancellationToken);

        budget.ChangeLimit(request.MonthlyLimit);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        dashboardCache.Invalidate(currentUser.UserId);

        return await ToDtoWithCurrentSpendingAsync(budget, budget.Category!, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var budget = await FindOwnedAsync(id, cancellationToken);
        budgets.Remove(budget);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        dashboardCache.Invalidate(currentUser.UserId);
    }

    internal static BudgetDto ToDto(Budget budget, Category category, decimal spent) => new(
        budget.Id,
        budget.CategoryId,
        category.Name,
        category.Color,
        budget.MonthlyLimit,
        spent,
        budget.MonthlyLimit - spent,
        budget.PercentUsed(spent),
        budget.Evaluate(spent));

    private async Task<BudgetDto> ToDtoWithCurrentSpendingAsync(Budget budget, Category category, CancellationToken cancellationToken)
    {
        var spending = await GetSpendingByCategoryAsync(currentUser.UserId, null, null, cancellationToken);
        return ToDto(budget, category, spending.GetValueOrDefault(budget.CategoryId));
    }

    private async Task<Dictionary<int, decimal>> GetSpendingByCategoryAsync(
        string userId, int? year, int? month, CancellationToken cancellationToken)
    {
        var (y, m) = DateHelpers.ResolveMonth(year, month, timeProvider);
        var totals = await expenses.GetCategoryTotalsAsync(
            userId, DateHelpers.FirstDayOfMonth(y, m), DateHelpers.LastDayOfMonth(y, m), cancellationToken);
        return totals.ToDictionary(t => t.CategoryId, t => t.Total);
    }

    private async Task<Budget> FindOwnedAsync(int id, CancellationToken cancellationToken) =>
        await budgets.GetAsync(id, currentUser.UserId, cancellationToken)
        ?? throw new NotFoundException("Budget", id);
}
