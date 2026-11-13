using Microsoft.AspNetCore.Identity;
using SpendSmart.Application.Abstractions;
using AppIdentityError = SpendSmart.Application.Abstractions.IdentityError;

namespace SpendSmart.Infrastructure.Identity;

internal sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IIdentityService
{
    public async Task<IdentityResultDto> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser { UserName = email, Email = email };
        var result = await userManager.CreateAsync(user, password);

        return result.Succeeded
            ? new IdentityResultDto(user.Id, [])
            : new IdentityResultDto(null, result.Errors.Select(e => new AppIdentityError(e.Code, e.Description)).ToList());
    }

    public async Task<AuthenticatedUser?> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        // Counts failures towards lockout, same as the cookie login does.
        var result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        return result.Succeeded ? new AuthenticatedUser(user.Id, user.Email!) : null;
    }
}
