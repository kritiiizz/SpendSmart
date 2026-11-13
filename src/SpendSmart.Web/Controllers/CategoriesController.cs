using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Exceptions;
using SpendSmart.Web.ViewModels;

namespace SpendSmart.Web.Controllers;

public sealed class CategoriesController(ICategoryService categories) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new CategoriesViewModel { Categories = await categories.ListAsync(cancellationToken) });

    [HttpPost]
    public async Task<IActionResult> Create(
        [Bind(nameof(CategoriesViewModel.Name), nameof(CategoriesViewModel.Color))] CategoriesViewModel model,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await categories.CreateAsync(new SaveCategoryRequest(model.Name, model.Color), cancellationToken);
                Flash($"Category '{model.Name.Trim()}' added.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ValidationException or DomainException)
            {
                AddErrors(ex);
            }
        }

        return View(nameof(Index), new CategoriesViewModel
        {
            Categories = await categories.ListAsync(cancellationToken),
            Name = model.Name,
            Color = model.Color,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await categories.DeleteAsync(id, cancellationToken);
            Flash("Category deleted.");
        }
        catch (ConflictException ex)
        {
            FlashError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }
}
