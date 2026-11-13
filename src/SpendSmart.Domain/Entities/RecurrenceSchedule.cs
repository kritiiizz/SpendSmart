using SpendSmart.Domain.Enums;

namespace SpendSmart.Domain.Entities;

public static class RecurrenceSchedule
{
    /// <summary>
    /// Calculates the occurrence after <paramref name="current"/>.
    /// <paramref name="anchorDay"/> is the day of month the schedule started on, which stops
    /// month-end drift: Jan 31 → Feb 28 → Mar 31, not Mar 28.
    /// </summary>
    public static DateOnly Next(DateOnly current, RecurrenceFrequency frequency, int anchorDay)
    {
        return frequency switch
        {
            RecurrenceFrequency.Weekly => current.AddDays(7),
            RecurrenceFrequency.Monthly => Anchored(current.AddMonths(1), anchorDay),
            RecurrenceFrequency.Yearly => Anchored(current.AddYears(1), anchorDay),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unknown frequency."),
        };
    }

    private static DateOnly Anchored(DateOnly date, int anchorDay)
    {
        var day = Math.Min(anchorDay, DateTime.DaysInMonth(date.Year, date.Month));
        return new DateOnly(date.Year, date.Month, day);
    }
}
