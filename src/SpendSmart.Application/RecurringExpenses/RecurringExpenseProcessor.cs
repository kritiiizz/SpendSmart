using Microsoft.Extensions.Logging;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.RecurringExpenses;

/// <summary>Turns due recurring templates into real expenses. Called by the background worker.</summary>
public interface IRecurringExpenseProcessor
{
    /// <returns>The number of expenses created.</returns>
    Task<int> ProcessDueAsync(CancellationToken cancellationToken = default);
}

public sealed class RecurringExpenseProcessor(
    IRecurringExpenseRepository recurringExpenses,
    IExpenseRepository expenses,
    IUnitOfWork unitOfWork,
    IDashboardCache dashboardCache,
    TimeProvider timeProvider,
    ILogger<RecurringExpenseProcessor> logger) : IRecurringExpenseProcessor
{
    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var today = timeProvider.Today();
        var due = await recurringExpenses.GetDueAsync(today, cancellationToken);

        var created = 0;
        var affectedUsers = new HashSet<string>();
        foreach (var template in due)
        {
            foreach (var date in template.CollectDueOccurrences(today))
            {
                expenses.Add(new Expense(
                    template.UserId, template.Amount, template.Description, date, template.CategoryId, template.Id));
                created++;
                affectedUsers.Add(template.UserId);
            }
        }

        if (created == 0)
        {
            return 0;
        }

        // Expenses and the advanced schedules are saved in one transaction, so a crash can't double-book.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        foreach (var userId in affectedUsers)
        {
            dashboardCache.Invalidate(userId);
        }

        logger.LogInformation(
            "Generated {ExpenseCount} recurring expenses from {TemplateCount} templates for {UserCount} users",
            created, due.Count, affectedUsers.Count);
        return created;
    }
}
