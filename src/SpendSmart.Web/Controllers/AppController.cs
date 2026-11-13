using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SpendSmart.Application.Categories;
using SpendSmart.Domain.Exceptions;

namespace SpendSmart.Web.Controllers;

/// <summary>
/// Base for MVC pages: cookie-authenticated, CSRF-protected on every POST,
/// and hidden from the OpenAPI document.
/// </summary>
[Authorize]
[AutoValidateAntiforgeryToken]
[ApiExplorerSettings(IgnoreApi = true)]
public abstract class AppController : Controller
{
    protected const string SuccessKey = "Success";
    protected const string ErrorKey = "Error";

    protected void Flash(string message) => TempData[SuccessKey] = message;

    protected void FlashError(string message) => TempData[ErrorKey] = message;

    /// <summary>Copies service-layer validation failures into ModelState so the form shows them inline.</summary>
    protected void AddErrors(Exception exception)
    {
        switch (exception)
        {
            case ValidationException validation:
                foreach (var error in validation.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }

                break;
            case DomainException domain:
                ModelState.AddModelError(string.Empty, domain.Message);
                break;
        }
    }

    protected static async Task<IEnumerable<SelectListItem>> CategoryOptionsAsync(
        ICategoryService categories, int? selected, CancellationToken cancellationToken)
    {
        var list = await categories.ListAsync(cancellationToken);
        return list.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == selected));
    }
}
