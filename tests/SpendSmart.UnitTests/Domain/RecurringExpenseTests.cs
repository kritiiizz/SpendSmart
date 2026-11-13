using SpendSmart.Domain.Entities;
using SpendSmart.Domain.Enums;
using SpendSmart.Domain.Exceptions;

namespace SpendSmart.UnitTests.Domain;

public class RecurringExpenseTests
{
    private static RecurringExpense MonthlyRent(DateOnly start) =>
        new("user", 1200m, "Rent", categoryId: 3, RecurrenceFrequency.Monthly, start);

    [Fact]
    public void CollectDueOccurrences_BeforeStartDate_ReturnsNothing()
    {
        var rent = MonthlyRent(new DateOnly(2026, 6, 1));

        var due = rent.CollectDueOccurrences(new DateOnly(2026, 5, 31));

        Assert.Empty(due);
        Assert.Equal(new DateOnly(2026, 6, 1), rent.NextOccurrence);
    }

    [Fact]
    public void CollectDueOccurrences_OnStartDate_ReturnsStartDateAndAdvances()
    {
        var rent = MonthlyRent(new DateOnly(2026, 6, 1));

        var due = rent.CollectDueOccurrences(new DateOnly(2026, 6, 1));

        Assert.Equal(new[] { new DateOnly(2026, 6, 1) }, due);
        Assert.Equal(new DateOnly(2026, 7, 1), rent.NextOccurrence);
    }

    [Fact]
    public void CollectDueOccurrences_CatchesUpOnMissedMonths()
    {
        var rent = MonthlyRent(new DateOnly(2026, 1, 1));

        var due = rent.CollectDueOccurrences(new DateOnly(2026, 3, 15));

        Assert.Equal(new[] { new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1) }, due);
        Assert.Equal(new DateOnly(2026, 4, 1), rent.NextOccurrence);
    }

    [Fact]
    public void CollectDueOccurrences_CalledTwiceSameDay_DoesNotDuplicate()
    {
        var rent = MonthlyRent(new DateOnly(2026, 1, 1));
        var today = new DateOnly(2026, 1, 20);

        rent.CollectDueOccurrences(today);
        var second = rent.CollectDueOccurrences(today);

        Assert.Empty(second);
    }

    [Fact]
    public void CollectDueOccurrences_WhenPaused_ReturnsNothing()
    {
        var rent = MonthlyRent(new DateOnly(2026, 1, 1));
        rent.Pause();

        Assert.Empty(rent.CollectDueOccurrences(new DateOnly(2026, 5, 1)));
        Assert.False(rent.IsActive);
    }

    [Fact]
    public void CollectDueOccurrences_IsCappedPerRun()
    {
        var weekly = new RecurringExpense("user", 5m, "Coffee beans", 1, RecurrenceFrequency.Weekly, new DateOnly(2020, 1, 1));

        var due = weekly.CollectDueOccurrences(new DateOnly(2026, 1, 1));

        Assert.Equal(RecurringExpense.MaxOccurrencesPerRun, due.Count);
    }

    [Fact]
    public void Resume_SkipsOccurrencesMissedWhilePaused()
    {
        var rent = MonthlyRent(new DateOnly(2026, 1, 1));
        rent.Pause();

        rent.Resume(new DateOnly(2026, 4, 10));

        Assert.True(rent.IsActive);
        Assert.Equal(new DateOnly(2026, 5, 1), rent.NextOccurrence);
        Assert.Empty(rent.CollectDueOccurrences(new DateOnly(2026, 4, 10)));
    }

    [Theory]
    [InlineData(0, "Rent")]
    [InlineData(10, "  ")]
    public void Constructor_RejectsInvalidValues(int amount, string description)
    {
        Assert.Throws<DomainException>(() =>
            new RecurringExpense("user", amount, description, 1, RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 1)));
    }
}
