using FluentValidation;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.RecurringExpenses;

public interface IRecurringExpenseService
{
    Task<IReadOnlyList<RecurringExpenseDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<RecurringExpenseDto> CreateAsync(CreateRecurringExpenseRequest request, CancellationToken cancellationToken = default);

    Task PauseAsync(int id, CancellationToken cancellationToken = default);

    Task ResumeAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class RecurringExpenseService(
    IRecurringExpenseRepository recurringExpenses,
    IExpenseRepository expenses,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDashboardCache dashboardCache,
    TimeProvider timeProvider,
    IValidator<CreateRecurringExpenseRequest> validator) : IRecurringExpenseService
{
    public async Task<IReadOnlyList<RecurringExpenseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await recurringExpenses.ListAsync(currentUser.UserId, cancellationToken);
        return list.Select(r => r.ToDto(r.Category!)).ToList();
    }

    public async Task<RecurringExpenseDto> CreateAsync(CreateRecurringExpenseRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var userId = currentUser.UserId;
        var category = await categories.GetAsync(request.CategoryId, userId, cancellationToken)
            ?? throw nameof(CreateRecurringExpenseRequest.CategoryId).ToValidationException("Category does not exist.");

        var recurring = new RecurringExpense(
            userId, request.Amount, request.Description, category.Id, request.Frequency, request.StartDate);
        recurringExpenses.Add(recurring);

        // Save first so the template has an id the generated expenses can point to.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Anything already due (e.g. a start date of today) is created now rather than waiting for the background job.
        var due = recurring.CollectDueOccurrences(timeProvider.Today());
        foreach (var date in due)
        {
            expenses.Add(new Expense(userId, recurring.Amount, recurring.Description, date, recurring.CategoryId, recurring.Id));
        }

        if (due.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            dashboardCache.Invalidate(userId);
        }

        return recurring.ToDto(category);
    }

    public async Task PauseAsync(int id, CancellationToken cancellationToken = default)
    {
        var recurring = await FindOwnedAsync(id, cancellationToken);
        recurring.Pause();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResumeAsync(int id, CancellationToken cancellationToken = default)
    {
        var recurring = await FindOwnedAsync(id, cancellationToken);
        recurring.Resume(timeProvider.Today());
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        // Expenses already generated are kept; their link to the template is cleared by the database.
        var recurring = await FindOwnedAsync(id, cancellationToken);
        recurringExpenses.Remove(recurring);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<RecurringExpense> FindOwnedAsync(int id, CancellationToken cancellationToken) =>
        await recurringExpenses.GetAsync(id, currentUser.UserId, cancellationToken)
        ?? throw new NotFoundException("Recurring expense", id);
}
