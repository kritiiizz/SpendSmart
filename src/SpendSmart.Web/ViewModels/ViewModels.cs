using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using SpendSmart.Application.Budgets;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Common;
using SpendSmart.Application.Expenses;
using SpendSmart.Application.RecurringExpenses;
using SpendSmart.Application.Reports;
using SpendSmart.Domain.Enums;

namespace SpendSmart.Web.ViewModels;

// Data annotations here only drive client-side validation for a quick UX.
// The authoritative rules are the FluentValidation validators in the Application layer.

public sealed class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Keep me signed in")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords don't match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class ExpenseFormViewModel
{
    public int? Id { get; set; }

    [Required, Range(0.01, 1_000_000)]
    [DataType(DataType.Currency)]
    public decimal? Amount { get; set; }

    [Required, StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required, Display(Name = "Category")]
    public int? CategoryId { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = [];

    public bool IsEdit => Id is not null;
}

public sealed class ExpenseListViewModel
{
    public required PagedResult<ExpenseDto> Result { get; init; }

    public required ExpenseQuery Query { get; init; }

    public required IReadOnlyList<CategoryDto> Categories { get; init; }

    public decimal PageTotal => Result.Items.Sum(e => e.Amount);
}

public sealed class BudgetFormViewModel
{
    public int? Id { get; set; }

    [Required, Display(Name = "Category")]
    public int? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    [Required, Range(0.01, 10_000_000), Display(Name = "Monthly limit")]
    public decimal? MonthlyLimit { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = [];

    public bool IsEdit => Id is not null;
}

public sealed class RecurringFormViewModel
{
    [Required, Range(0.01, 1_000_000)]
    public decimal? Amount { get; set; }

    [Required, StringLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required, Display(Name = "Category")]
    public int? CategoryId { get; set; }

    [Required]
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;

    [Required, DataType(DataType.Date), Display(Name = "First payment")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public IEnumerable<SelectListItem> Categories { get; set; } = [];
}

public sealed class CategoriesViewModel
{
    public IReadOnlyList<CategoryDto> Categories { get; init; } = [];

    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = "#0d6efd";
}

public sealed record BudgetsViewModel(IReadOnlyList<BudgetDto> Budgets, int Year, int Month);

public sealed record RecurringListViewModel(IReadOnlyList<RecurringExpenseDto> Items);

public sealed record DashboardViewModel(DashboardDto Dashboard)
{
    public DateOnly MonthStart => new(Dashboard.Year, Dashboard.Month, 1);
}

public sealed record ErrorViewModel(int StatusCode, string? RequestId)
{
    public string Title => StatusCode switch
    {
        404 => "Page not found",
        403 => "Access denied",
        _ => "Something went wrong",
    };

    public string Message => StatusCode switch
    {
        404 => "The page or item you're looking for doesn't exist, or it belongs to another account.",
        403 => "You don't have permission to view this page.",
        _ => "An unexpected error occurred. It has been logged and we'll look into it.",
    };
}
