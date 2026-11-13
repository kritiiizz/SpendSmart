using SpendSmart.Domain.Common;
using SpendSmart.Domain.Enums;

namespace SpendSmart.Domain.Entities;

/// <summary>A monthly spending limit for one category.</summary>
public sealed class Budget : Entity, IOwnedByUser
{
    /// <summary>Share of the limit at which the user is warned (80%).</summary>
    public const decimal WarningThreshold = 0.8m;

    private Budget()
    {
    }

    public Budget(string userId, int categoryId, decimal monthlyLimit)
    {
        UserId = Guard.NotBlank(userId, nameof(UserId), 450);
        CategoryId = categoryId;
        ChangeLimit(monthlyLimit);
    }

    public string UserId { get; private set; } = null!;

    public int CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public decimal MonthlyLimit { get; private set; }

    public void ChangeLimit(decimal monthlyLimit) =>
        MonthlyLimit = Guard.Positive(monthlyLimit, nameof(MonthlyLimit));

    /// <summary>
    /// Over the limit is <see cref="BudgetStatus.Exceeded"/>; from 80% up to and including
    /// the limit is <see cref="BudgetStatus.Warning"/>; anything below is on track.
    /// </summary>
    public BudgetStatus Evaluate(decimal spent)
    {
        if (spent > MonthlyLimit)
        {
            return BudgetStatus.Exceeded;
        }

        return spent >= MonthlyLimit * WarningThreshold ? BudgetStatus.Warning : BudgetStatus.OnTrack;
    }

    /// <summary>Spent as a percentage of the limit, rounded to one decimal place.</summary>
    public decimal PercentUsed(decimal spent) => Math.Round(spent / MonthlyLimit * 100m, 1);
}
