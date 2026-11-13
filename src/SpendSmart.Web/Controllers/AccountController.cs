using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using SpendSmart.Api;
using SpendSmart.Application.Accounts;
using SpendSmart.Infrastructure.Identity;
using SpendSmart.Infrastructure.Persistence;
using SpendSmart.Web.ViewModels;

namespace SpendSmart.Web.Controllers;

[AllowAnonymous]
public sealed class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IAccountService accounts,
    IOptions<SeedOptions> seedOptions,
    ILogger<AccountController> logger) : AppController
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return LocalRedirectOrHome(model.ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Too many failed attempts. Try again in a few minutes."
            : "Incorrect email or password.");
        return View(model);
    }

    /// <summary>One-click sign-in to the seeded demo account, so recruiters don't need to register.</summary>
    [HttpPost]
    public async Task<IActionResult> Demo()
    {
        var settings = seedOptions.Value;
        var result = await signInManager.PasswordSignInAsync(settings.DemoEmail, settings.DemoPassword, false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            logger.LogWarning("Demo sign-in failed; is seeding enabled?");
            FlashError("The demo account isn't available right now. Please register instead.");
            return RedirectToAction(nameof(Register));
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var userId = await accounts.RegisterAsync(new RegisterRequest(model.Email, model.Password), cancellationToken);
            var user = await userManager.FindByIdAsync(userId);
            await signInManager.SignInAsync(user!, isPersistent: false);
            Flash("Welcome to SpendSmart! We've added some default categories to get you started.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
        catch (ValidationException ex)
        {
            AddErrors(ex);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private IActionResult LocalRedirectOrHome(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(HomeController.Index), "Home");
}
