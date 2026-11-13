using SpendSmart.Domain.Common;
using SpendSmart.Domain.Enums;

namespace SpendSmart.Domain.Entities;

/// <summary>A template (rent, subscriptions…) from which real expenses are generated on schedule.</summary>
public sealed class RecurringExpense : Entity, IOwnedByUser
{
    /// <summary>Safety cap so a template far in the past can't flood the database in one run.</summary>
    public const int MaxOccurrencesPerRun = 60;

    private RecurringExpense()
    {
    }

    public RecurringExpense(
        string userId,
        decimal amount,
        string description,
        int categoryId,
        RecurrenceFrequency frequency,
        DateOnly startDate)
    {
        UserId = Guard.NotBlank(userId, nameof(UserId), 450);
        Amount = Guard.Positive(amount, nameof(Amount));
        Description = Guard.NotBlank(description, nameof(Description), Expense.DescriptionMaxLength);
        CategoryId = categoryId;
        Frequency = frequency;
        StartDate = startDate;
        NextOccurrence = startDate;
        IsActive = true;
    }

    public string UserId { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public string Description { get; private set; } = null!;

    public int CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public RecurrenceFrequency Frequency { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly NextOccurrence { get; private set; }

    public bool IsActive { get; private set; }

    public void Pause() => IsActive = false;

    /// <summary>Resumes the schedule without back-filling the occurrences missed while paused.</summary>
    public void Resume(DateOnly today)
    {
        IsActive = true;
        while (NextOccurrence < today)
        {
            NextOccurrence = RecurrenceSchedule.Next(NextOccurrence, Frequency, StartDate.Day);
        }
    }

    /// <summary>
    /// Returns every occurrence due on or before <paramref name="today"/> and moves the schedule forward,
    /// so calling it twice on the same day never produces duplicates.
    /// </summary>
    public IReadOnlyList<DateOnly> CollectDueOccurrences(DateOnly today)
    {
        var due = new List<DateOnly>();
        if (!IsActive)
        {
            return due;
        }

        while (NextOccurrence <= today && due.Count < MaxOccurrencesPerRun)
        {
            due.Add(NextOccurrence);
            NextOccurrence = RecurrenceSchedule.Next(NextOccurrence, Frequency, StartDate.Day);
        }

        return due;
    }
}
