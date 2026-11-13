using FluentValidation;
using SpendSmart.Domain.Enums;

namespace SpendSmart.Application.Budgets;

public sealed record BudgetDto(
    int Id,
    int CategoryId,
    string CategoryName,
    string CategoryColor,
    decimal MonthlyLimit,
    decimal Spent,
    decimal Remaining,
    decimal PercentUsed,
    BudgetStatus Status);

public sealed record CreateBudgetRequest(int CategoryId, decimal MonthlyLimit);

public sealed record UpdateBudgetRequest(decimal MonthlyLimit);

public sealed class CreateBudgetRequestValidator : AbstractValidator<CreateBudgetRequest>
{
    public CreateBudgetRequestValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Choose a category.");
        RuleFor(x => x.MonthlyLimit).GreaterThan(0).LessThanOrEqualTo(10_000_000).PrecisionScale(18, 2, true);
    }
}

public sealed class UpdateBudgetRequestValidator : AbstractValidator<UpdateBudgetRequest>
{
    public UpdateBudgetRequestValidator()
    {
        RuleFor(x => x.MonthlyLimit).GreaterThan(0).LessThanOrEqualTo(10_000_000).PrecisionScale(18, 2, true);
    }
}
