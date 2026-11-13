using SpendSmart.Domain.Entities;
using SpendSmart.Domain.Enums;

namespace SpendSmart.UnitTests.Domain;

public class RecurrenceScheduleTests
{
    [Fact]
    public void Weekly_AddsSevenDays()
    {
        var next = RecurrenceSchedule.Next(new DateOnly(2026, 12, 28), RecurrenceFrequency.Weekly, anchorDay: 28);

        Assert.Equal(new DateOnly(2027, 1, 4), next);
    }

    [Theory]
    [InlineData("2026-01-31", 31, "2026-02-28")]
    [InlineData("2026-02-28", 31, "2026-03-31")] // returns to the anchor day instead of drifting to the 28th
    [InlineData("2028-01-31", 31, "2028-02-29")] // leap year
    [InlineData("2026-04-30", 30, "2026-05-30")]
    [InlineData("2026-12-15", 15, "2027-01-15")]
    public void Monthly_KeepsAnchorDayWithoutDrift(string current, int anchorDay, string expected)
    {
        var next = RecurrenceSchedule.Next(DateOnly.Parse(current), RecurrenceFrequency.Monthly, anchorDay);

        Assert.Equal(DateOnly.Parse(expected), next);
    }

    [Theory]
    [InlineData("2028-02-29", 29, "2029-02-28")]
    [InlineData("2029-02-28", 29, "2030-02-28")]
    [InlineData("2031-02-28", 29, "2032-02-29")]
    public void Yearly_HandlesLeapDays(string current, int anchorDay, string expected)
    {
        var next = RecurrenceSchedule.Next(DateOnly.Parse(current), RecurrenceFrequency.Yearly, anchorDay);

        Assert.Equal(DateOnly.Parse(expected), next);
    }
}
