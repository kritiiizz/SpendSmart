using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Budgets;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Application.Reports;
using SpendSmart.Domain.Enums;
using SpendSmart.UnitTests.TestSupport;

namespace SpendSmart.UnitTests.Application;

public class ReportServiceTests
{
    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly IBudgetService _budgets = Substitute.For<IBudgetService>();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestData.UserId);
        var cache = new DashboardCache(new MemoryCache(new MemoryCacheOptions()));
        _sut = new ReportService(_expenses, _budgets, currentUser, cache, new FixedTimeProvider(2026, 3, 15));

        _expenses.GetCategoryTotalsAsync(default!, default, default, default).ReturnsForAnyArgs(new List<CategoryTotal>
        {
            new(1, "Groceries", "#198754", 300m),
            new(2, "Dining Out", "#fd7e14", 100m),
        });
        _expenses.GetMonthlyTotalsAsync(default!, default, default, default).ReturnsForAnyArgs(new List<MonthlyTotal>
        {
            new(2025, 12, 350m),
            new(2026, 2, 320m),
            new(2026, 3, 400m),
        });
        _budgets.ListAsync(default, default, default).ReturnsForAnyArgs(new List<BudgetDto>
        {
            new(1, 1, "Groceries", "#198754", 500m, 300m, 200m, 60m, BudgetStatus.OnTrack),
            new(2, 2, "Dining Out", "#fd7e14", 110m, 100m, 10m, 90.9m, BudgetStatus.Warning),
        });
    }

    [Fact]
    public async Task GetDashboardAsync_BuildsTotalsTrendAndAlerts()
    {
        var dashboard = await _sut.GetDashboardAsync();

        Assert.Equal((2026, 3), (dashboard.Year, dashboard.Month));
        Assert.Equal(400m, dashboard.TotalThisMonth);
        Assert.Equal(320m, dashboard.TotalLastMonth);
        Assert.Equal(25m, dashboard.ChangeFromLastMonthPercent);
        Assert.Equal(75m, dashboard.SpendingByCategory[0].Share);
        Assert.Equal(ReportService.TrendMonths, dashboard.MonthlyTrend.Count);
        Assert.Equal("Oct 2025", dashboard.MonthlyTrend[0].Label);
        Assert.Equal("Dining Out", Assert.Single(dashboard.BudgetAlerts).CategoryName);
    }

    [Fact]
    public async Task GetDashboardAsync_SecondCall_IsServedFromCache()
    {
        await _sut.GetDashboardAsync(2026, 3);
        await _sut.GetDashboardAsync(2026, 3);

        await _expenses.ReceivedWithAnyArgs(1).GetMonthlyTotalsAsync(default!, default, default, default);
    }

    [Fact]
    public void BuildTrend_FillsMonthsWithoutSpendingWithZero()
    {
        var trend = ReportService.BuildTrend(new DateOnly(2025, 11, 1), new List<MonthlyTotal> { new(2026, 1, 50m) });

        Assert.Equal(new[] { 0m, 0m, 50m, 0m, 0m, 0m }, trend.Select(t => t.Total));
        Assert.Equal("Apr 2026", trend[^1].Label);
    }

    [Theory]
    [InlineData(0.0, 100.0, null)]
    [InlineData(200.0, 100.0, -50.0)]
    [InlineData(100.0, 133.33, 33.3)]
    public void PercentChange_HandlesZeroAndRounds(double previous, double current, double? expected)
    {
        Assert.Equal((decimal?)expected, ReportService.PercentChange((decimal)previous, (decimal)current));
    }
}
