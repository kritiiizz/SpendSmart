using FluentValidation;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.Expenses;

public sealed class SaveExpenseRequestValidator : AbstractValidator<SaveExpenseRequest>
{
    public static readonly DateOnly EarliestDate = new(2000, 1, 1);

    public SaveExpenseRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000).WithMessage("Amount must be 1,000,000 or less.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("Amount can have at most two decimal places.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(Expense.DescriptionMaxLength);

        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(EarliestDate).WithMessage("Date must be in the year 2000 or later.")
            .Must(date => date <= timeProvider.Today().AddDays(1))
            .WithMessage("Date can't be in the future. Use a recurring expense for planned payments.");

        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Choose a category.");
    }
}

public sealed class ExpenseQueryValidator : AbstractValidator<ExpenseQuery>
{
    public ExpenseQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, ExpenseQuery.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Category).MaximumLength(Category.NameMaxLength);
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.SortDirection).IsInEnum();

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must be on or after 'From'.");

        RuleFor(x => x.MaxAmount)
            .GreaterThanOrEqualTo(x => x.MinAmount)
            .When(x => x.MinAmount is not null && x.MaxAmount is not null)
            .WithMessage("'MaxAmount' must be greater than or equal to 'MinAmount'.");
    }
}
