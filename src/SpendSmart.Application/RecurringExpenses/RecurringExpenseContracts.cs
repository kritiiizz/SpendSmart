using FluentValidation;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;
using SpendSmart.Domain.Enums;

namespace SpendSmart.Application.RecurringExpenses;

public sealed record RecurringExpenseDto(
    int Id,
    decimal Amount,
    string Description,
    int CategoryId,
    string CategoryName,
    RecurrenceFrequency Frequency,
    DateOnly StartDate,
    DateOnly NextOccurrence,
    bool IsActive);

public sealed record CreateRecurringExpenseRequest(
    decimal Amount,
    string Description,
    int CategoryId,
    RecurrenceFrequency Frequency,
    DateOnly StartDate);

public sealed class CreateRecurringExpenseRequestValidator : AbstractValidator<CreateRecurringExpenseRequest>
{
    public CreateRecurringExpenseRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000).PrecisionScale(18, 2, true);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(Expense.DescriptionMaxLength);
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Choose a category.");
        RuleFor(x => x.Frequency).IsInEnum();
        RuleFor(x => x.StartDate)
            .Must(date => date >= timeProvider.Today().AddMonths(-3))
            .WithMessage("Start date can be at most three months in the past.")
            .Must(date => date <= timeProvider.Today().AddYears(1))
            .WithMessage("Start date can be at most one year in the future.");
    }
}

internal static class RecurringExpenseMappings
{
    public static RecurringExpenseDto ToDto(this RecurringExpense r, Category category) => new(
        r.Id, r.Amount, r.Description, category.Id, category.Name, r.Frequency, r.StartDate, r.NextOccurrence, r.IsActive);
}
