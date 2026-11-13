using NSubstitute;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Budgets;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;
using SpendSmart.Domain.Enums;
using SpendSmart.UnitTests.TestSupport;

namespace SpendSmart.UnitTests.Application;

public class BudgetServiceTests
{
    private readonly IBudgetRepository _budgets = Substitute.For<IBudgetRepository>();
    private readonly ICategoryRepository _categories = Substitute.For<ICategoryRepository>();
    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDashboardCache _cache = Substitute.For<IDashboardCache>();
    private readonly BudgetService _sut;

    public BudgetServiceTests()
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns(TestData.UserId);
        _sut = new BudgetService(
            _budgets, _categories, _expenses, _unitOfWork, currentUser, _cache,
            new FixedTimeProvider(2026, 3, 15), new CreateBudgetRequestValidator(), new UpdateBudgetRequestValidator());
    }

    [Fact]
    public async Task ListAsync_CombinesLimitsWithMonthSpending_SortedByUsage()
    {
        var groceries = TestData.Category(1, "Groceries");
        var dining = TestData.Category(2, "Dining Out");
        var fun = TestData.Category(3, "Entertainment");
        _budgets.ListAsync(TestData.UserId, Arg.Any<CancellationToken>()).Returns(new List<Budget>
        {
            new Budget(TestData.UserId, 1, 400m).WithId(10).WithCategory(groceries),
            new Budget(TestData.UserId, 2, 150m).WithId(11).WithCategory(dining),
            new Budget(TestData.UserId, 3, 50m).WithId(12).WithCategory(fun),
        });
        _expenses.GetCategoryTotalsAsync(TestData.UserId, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), Arg.Any<CancellationToken>())
            .Returns(new List<CategoryTotal>
            {
                new(1, "Groceries", "#000000", 100m),
                new(2, "Dining Out", "#000000", 135m),
                new(3, "Entertainment", "#000000", 75m),
            });

        var result = await _sut.ListAsync();

        Assert.Equal(new[] { "Entertainment", "Dining Out", "Groceries" }, result.Select(b => b.CategoryName));
        Assert.Equal(BudgetStatus.Exceeded, result[0].Status);
        Assert.Equal(-25m, result[0].Remaining);
        Assert.Equal(BudgetStatus.Warning, result[1].Status);
        Assert.Equal(90m, result[1].PercentUsed);
        Assert.Equal(BudgetStatus.OnTrack, result[2].Status);
    }

    [Fact]
    public async Task ListAsync_CategoryWithoutSpending_CountsAsZero()
    {
        _budgets.ListAsync(TestData.UserId, Arg.Any<CancellationToken>()).Returns(new List<Budget>
        {
            new Budget(TestData.UserId, 1, 100m).WithId(10).WithCategory(TestData.Category(1)),
        });
        _expenses.GetCategoryTotalsAsync(default!, default, default, default).ReturnsForAnyArgs(new List<CategoryTotal>());

        var budget = Assert.Single(await _sut.ListAsync());

        Assert.Equal(0m, budget.Spent);
        Assert.Equal(BudgetStatus.OnTrack, budget.Status);
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryAlreadyHasBudget_ThrowsConflict()
    {
        _categories.GetAsync(1, TestData.UserId, Arg.Any<CancellationToken>()).Returns(TestData.Category(1));
        _budgets.ExistsForCategoryAsync(TestData.UserId, 1, Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(new CreateBudgetRequest(1, 300m)));

        _budgets.DidNotReceive().Add(Arg.Any<Budget>());
    }

    [Fact]
    public async Task CreateAsync_AddsBudgetAndReturnsCurrentStatus()
    {
        _categories.GetAsync(1, TestData.UserId, Arg.Any<CancellationToken>()).Returns(TestData.Category(1));
        _expenses.GetCategoryTotalsAsync(default!, default, default, default)
            .ReturnsForAnyArgs(new List<CategoryTotal> { new(1, "Groceries", "#000000", 250m) });

        var result = await _sut.CreateAsync(new CreateBudgetRequest(1, 300m));

        _budgets.Received(1).Add(Arg.Is<Budget>(b => b.CategoryId == 1 && b.MonthlyLimit == 300m));
        _cache.Received(1).Invalidate(TestData.UserId);
        Assert.Equal(BudgetStatus.Warning, result.Status);
    }
}
