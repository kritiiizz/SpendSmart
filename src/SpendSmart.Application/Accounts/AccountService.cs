using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using SpendSmart.Application.Abstractions;
using SpendSmart.Application.Categories;
using SpendSmart.Domain.Entities;

namespace SpendSmart.Application.Accounts;

public sealed record RegisterRequest(string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public interface IAccountService
{
    /// <summary>Creates the account and its default categories. Returns the new user's id.</summary>
    Task<string> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthenticatedUser?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public sealed class AccountService(
    IIdentityService identityService,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    ILogger<AccountService> logger) : IAccountService
{
    public async Task<string> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var result = await identityService.CreateUserAsync(request.Email, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure(
                e.Code.Contains("Password", StringComparison.OrdinalIgnoreCase) ? nameof(RegisterRequest.Password) : nameof(RegisterRequest.Email),
                e.Description)));
        }

        var userId = result.UserId!;
        foreach (var (name, color) in DefaultCategories.All)
        {
            categories.Add(new Category(userId, name, color));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Registered user {UserId}", userId);
        return userId;
    }

    public async Task<AuthenticatedUser?> AuthenticateAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await identityService.CheckCredentialsAsync(request.Email, request.Password, cancellationToken);
    }
}
