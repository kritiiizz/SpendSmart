using FluentValidation;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.Categories;

public sealed record CategoryDto(int Id, string Name, string Color);

public sealed record SaveCategoryRequest(string Name, string? Color);

public sealed class SaveCategoryRequestValidator : AbstractValidator<SaveCategoryRequest>
{
    public SaveCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Category.NameMaxLength);
        RuleFor(x => x.Color)
            .Matches("^#[0-9a-fA-F]{6}$").WithMessage("Color must be a hex value such as #0d6efd.")
            .When(x => !string.IsNullOrEmpty(x.Color));
    }
}

/// <summary>Categories every new account starts with.</summary>
public static class DefaultCategories
{
    public static readonly IReadOnlyList<(string Name, string Color)> All =
    [
        ("Groceries", "#198754"),
        ("Dining Out", "#fd7e14"),
        ("Housing", "#6f42c1"),
        ("Utilities", "#20c997"),
        ("Transport", "#0dcaf0"),
        ("Subscriptions", "#0d6efd"),
        ("Entertainment", "#d63384"),
        ("Health", "#dc3545"),
        ("Shopping", "#ffc107"),
        ("Other", "#6c757d"),
    ];
}

internal static class CategoryMappings
{
    public static CategoryDto ToDto(this Category category) => new(category.Id, category.Name, category.Color);
}
