using FluentValidation;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Caching;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class CategoryService(
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IDashboardCache dashboardCache,
    IValidator<SaveCategoryRequest> validator) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await categories.ListAsync(currentUser.UserId, cancellationToken);
        return list.Select(c => c.ToDto()).ToList();
    }

    public async Task<CategoryDto> GetAsync(int id, CancellationToken cancellationToken = default) =>
        (await FindOwnedAsync(id, cancellationToken)).ToDto();

    public async Task<CategoryDto> CreateAsync(SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var userId = currentUser.UserId;
        await EnsureUniqueNameAsync(userId, request.Name, null, cancellationToken);

        var category = new Category(userId, request.Name, string.IsNullOrEmpty(request.Color) ? null : request.Color);
        categories.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return category.ToDto();
    }

    public async Task<CategoryDto> UpdateAsync(int id, SaveCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var category = await FindOwnedAsync(id, cancellationToken);
        await EnsureUniqueNameAsync(currentUser.UserId, request.Name, id, cancellationToken);

        category.Rename(request.Name, string.IsNullOrEmpty(request.Color) ? null : request.Color);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        dashboardCache.Invalidate(currentUser.UserId);
        return category.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await FindOwnedAsync(id, cancellationToken);
        if (await categories.IsInUseAsync(id, cancellationToken))
        {
            throw new ConflictException(
                $"'{category.Name}' is used by expenses, budgets or recurring expenses. Reassign or delete those first.");
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> FindOwnedAsync(int id, CancellationToken cancellationToken) =>
        await categories.GetAsync(id, currentUser.UserId, cancellationToken)
        ?? throw new NotFoundException("Category", id);

    private async Task EnsureUniqueNameAsync(string userId, string name, int? excludingId, CancellationToken cancellationToken)
    {
        if (await categories.NameExistsAsync(userId, name.Trim(), excludingId, cancellationToken))
        {
            throw nameof(SaveCategoryRequest.Name).ToValidationException($"You already have a category called '{name.Trim()}'.");
        }
    }
}
