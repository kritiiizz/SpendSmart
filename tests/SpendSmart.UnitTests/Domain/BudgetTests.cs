using SpendSmart.Domain.Entities;
using SpendSmart.Domain.Enums;
using SpendSmart.Domain.Exceptions;

namespace SpendSmart.UnitTests.Domain;

public class BudgetTests
{
    [Theory]
    [InlineData(0, BudgetStatus.OnTrack)]
    [InlineData(79.99, BudgetStatus.OnTrack)]
    [InlineData(80, BudgetStatus.Warning)]
    [InlineData(95, BudgetStatus.Warning)]
    [InlineData(100, BudgetStatus.Warning)]
    [InlineData(100.01, BudgetStatus.Exceeded)]
    [InlineData(250, BudgetStatus.Exceeded)]
    public void Evaluate_ReturnsStatusBasedOnShareOfLimit(double spent, BudgetStatus expected)
    {
        var budget = new Budget("user", categoryId: 1, monthlyLimit: 100m);

        Assert.Equal(expected, budget.Evaluate((decimal)spent));
    }

    [Fact]
    public void PercentUsed_RoundsToOneDecimal()
    {
        var budget = new Budget("user", 1, 150m);

        Assert.Equal(86.7m, budget.PercentUsed(130m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_RejectsNonPositiveLimit(int limit)
    {
        Assert.Throws<DomainException>(() => new Budget("user", 1, limit));
    }

    [Fact]
    public void ChangeLimit_UpdatesLimit()
    {
        var budget = new Budget("user", 1, 100m);

        budget.ChangeLimit(250m);

        Assert.Equal(250m, budget.MonthlyLimit);
        Assert.Equal(BudgetStatus.OnTrack, budget.Evaluate(150m));
    }
}
