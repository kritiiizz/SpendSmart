namespace SpendSmart.Application.Common;

public static class DateHelpers
{
    public static DateOnly Today(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    public static DateOnly FirstDayOfMonth(int year, int month) => new(year, month, 1);

    public static DateOnly LastDayOfMonth(int year, int month) =>
        new(year, month, DateTime.DaysInMonth(year, month));

    /// <summary>Uses the given month if both parts are supplied, otherwise the current month.</summary>
    public static (int Year, int Month) ResolveMonth(int? year, int? month, TimeProvider timeProvider)
    {
        if (year is >= 2000 and <= 2100 && month is >= 1 and <= 12)
        {
            return (year.Value, month.Value);
        }

        var today = timeProvider.Today();
        return (today.Year, today.Month);
    }
}
