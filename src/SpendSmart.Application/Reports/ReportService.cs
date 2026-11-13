using System.Globalization;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Budgets;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Enums;

namespace SpendSmart.Application.Reports;

public interface IReportService
{
    /// <summary>Summary for one month (defaults to the current month). Cached per user.</summary>
    Task<DashboardDto> GetDashboardAsync(int? year = null, int? month = null, CancellationToken cancellationToken = default);
}

public sealed class ReportService(
    IExpenseRepository expenses,
    IBudgetService budgets,
    ICurrentUser currentUser,
    IDashboardCache cache,
    TimeProvider timeProvider) : IReportService
{
    public const int TrendMonths = 6;

    public Task<DashboardDto> GetDashboardAsync(int? year = null, int? month = null, CancellationToken cancellationToken = default)
    {
        var (y, m) = DateHelpers.ResolveMonth(year, month, timeProvider);
        return cache.GetOrCreateAsync(
            currentUser.UserId,
            $"{y:D4}-{m:D2}",
            ct => BuildDashboardAsync(y, m, ct),
            cancellationToken);
    }

    private async Task<DashboardDto> BuildDashboardAsync(int year, int month, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var monthStart = DateHelpers.FirstDayOfMonth(year, month);
        var monthEnd = DateHelpers.LastDayOfMonth(year, month);
        var trendStart = monthStart.AddMonths(-(TrendMonths - 1));

        var byCategory = await expenses.GetCategoryTotalsAsync(userId, monthStart, monthEnd, cancellationToken);
        var monthly = await expenses.GetMonthlyTotalsAsync(userId, trendStart, monthEnd, cancellationToken);
        var budgetStatuses = await budgets.ListAsync(year, month, cancellationToken);

        var trend = BuildTrend(trendStart, monthly);
        var thisMonth = trend[^1].Total;
        var lastMonth = trend[^2].Total;

        return new DashboardDto(
            year,
            month,
            thisMonth,
            lastMonth,
            PercentChange(lastMonth, thisMonth),
            byCategory
                .Select(c => new CategorySpendingDto(
                    c.CategoryId, c.CategoryName, c.Color, c.Total,
                    thisMonth == 0 ? 0 : Math.Round(c.Total / thisMonth * 100m, 1)))
                .OrderByDescending(c => c.Total)
                .ToList(),
            trend,
            budgetStatuses.Where(b => b.Status != BudgetStatus.OnTrack).ToList());
    }

    /// <summary>One entry per month in the window, with zero for months that had no spending.</summary>
    internal static IReadOnlyList<MonthlySpendingDto> BuildTrend(DateOnly start, IReadOnlyList<MonthlyTotal> totals)
    {
        var lookup = totals.ToDictionary(t => (t.Year, t.Month), t => t.Total);
        return Enumerable.Range(0, TrendMonths)
            .Select(i => start.AddMonths(i))
            .Select(d => new MonthlySpendingDto(
                d.Year,
                d.Month,
                d.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                lookup.GetValueOrDefault((d.Year, d.Month))))
            .ToList();
    }

    /// <summary>Null when there's nothing to compare against (avoids a meaningless "+∞%").</summary>
    internal static decimal? PercentChange(decimal previous, decimal current) =>
        previous == 0 ? null : Math.Round((current - previous) / previous * 100m, 1);
}
