using SpendSmart.Domain.Common;

namespace SpendSmart.Domain.Entities;

public sealed class Expense : Entity, IOwnedByUser
{
    public const int DescriptionMaxLength = 200;

    private Expense()
    {
    }

    public Expense(
        string userId,
        decimal amount,
        string description,
        DateOnly date,
        int categoryId,
        int? recurringExpenseId = null)
    {
        UserId = Guard.NotBlank(userId, nameof(UserId), 450);
        RecurringExpenseId = recurringExpenseId;
        Update(amount, description, date, categoryId);
    }

    public string UserId { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public string Description { get; private set; } = null!;

    public DateOnly Date { get; private set; }

    public int CategoryId { get; private set; }

    public Category? Category { get; private set; }

    /// <summary>Set when the expense was generated from a recurring template.</summary>
    public int? RecurringExpenseId { get; private set; }

    public void Update(decimal amount, string description, DateOnly date, int categoryId)
    {
        Amount = Guard.Positive(amount, nameof(Amount));
        Description = Guard.NotBlank(description, nameof(Description), DescriptionMaxLength);
        Date = date;
        CategoryId = categoryId;
    }
}
