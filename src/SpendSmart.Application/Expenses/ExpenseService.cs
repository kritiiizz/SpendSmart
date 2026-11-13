using FluentValidation;
using Microsoft.Extensions.Logging;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.Expenses;

public interface IExpenseService
{
    Task<PagedResult<ExpenseDto>> ListAsync(ExpenseQuery query, CancellationToken cancellationToken = default);

    Task<ExpenseDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<ExpenseDto> CreateAsync(SaveExpenseRequest request, CancellationToken cancellationToken = default);

    Task<ExpenseDto> UpdateAsync(int id, SaveExpenseRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class ExpenseService(
    IExpenseRepository expenses,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDashboardCache dashboardCache,
    IValidator<SaveExpenseRequest> saveValidator,
    IValidator<ExpenseQuery> queryValidator,
    ILogger<ExpenseService> logger) : IExpenseService
{
    public async Task<PagedResult<ExpenseDto>> ListAsync(ExpenseQuery query, CancellationToken cancellationToken = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var (items, totalCount) = await expenses.ListAsync(currentUser.UserId, query, cancellationToken);
        return new PagedResult<ExpenseDto>(
            items.Select(e => e.ToDto()).ToList(), query.Page, query.PageSize, totalCount);
    }

    public async Task<ExpenseDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var expense = await FindOwnedAsync(id, cancellationToken);
        return expense.ToDto();
    }

    public async Task<ExpenseDto> CreateAsync(SaveExpenseRequest request, CancellationToken cancellationToken = default)
    {
        await saveValidator.ValidateAndThrowAsync(request, cancellationToken);
        var userId = currentUser.UserId;
        var category = await GetOwnedCategoryAsync(request.CategoryId, userId, cancellationToken);

        var expense = new Expense(userId, request.Amount, request.Description, request.Date, category.Id);
        expenses.Add(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        dashboardCache.Invalidate(userId);

        logger.LogInformation("User {UserId} created expense {ExpenseId} for {Amount}", userId, expense.Id, expense.Amount);
        return expense.ToDto(category);
    }

    public async Task<ExpenseDto> UpdateAsync(int id, SaveExpenseRequest request, CancellationToken cancellationToken = default)
    {
        await saveValidator.ValidateAndThrowAsync(request, cancellationToken);
        var userId = currentUser.UserId;
        var expense = await FindOwnedAsync(id, cancellationToken);
        var category = await GetOwnedCategoryAsync(request.CategoryId, userId, cancellationToken);

        expense.Update(request.Amount, request.Description, request.Date, category.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        dashboardCache.Invalidate(userId);

        return expense.ToDto(category);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var expense = await FindOwnedAsync(id, cancellationToken);
        expenses.Remove(expense);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        dashboardCache.Invalidate(currentUser.UserId);

        logger.LogInformation("User {UserId} deleted expense {ExpenseId}", currentUser.UserId, id);
    }

    private async Task<Expense> FindOwnedAsync(int id, CancellationToken cancellationToken) =>
        await expenses.GetAsync(id, currentUser.UserId, cancellationToken)
        ?? throw new NotFoundException("Expense", id);

    private async Task<Category> GetOwnedCategoryAsync(int categoryId, string userId, CancellationToken cancellationToken) =>
        await categories.GetAsync(categoryId, userId, cancellationToken)
        ?? throw nameof(SaveExpenseRequest.CategoryId).ToValidationException("Category does not exist.");
}
