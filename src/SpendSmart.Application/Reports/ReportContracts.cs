using SpendSmart.Application.Budgets;

namespace SpendSmart.Application.Reports;

public sealed record CategorySpendingDto(int CategoryId, string CategoryName, string Color, decimal Total, decimal Share);

public sealed record MonthlySpendingDto(int Year, int Month, string Label, decimal Total);

public sealed record DashboardDto(
    int Year,
    int Month,
    decimal TotalThisMonth,
    decimal TotalLastMonth,
    decimal? ChangeFromLastMonthPercent,
    IReadOnlyList<CategorySpendingDto> SpendingByCategory,
    IReadOnlyList<MonthlySpendingDto> MonthlyTrend,
    IReadOnlyList<BudgetDto> BudgetAlerts);
