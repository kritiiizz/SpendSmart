using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Budgets;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Common;
using SpendSmart.Domain.Exceptions;
using SpendSmart.Web.ViewModels;

namespace SpendSmart.Web.Controllers;

public sealed class BudgetsController(IBudgetService budgets, ICategoryService categories, TimeProvider timeProvider) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var today = timeProvider.Today();
        var list = await budgets.ListAsync(today.Year, today.Month, cancellationToken);
        return View(new BudgetsViewModel(list, today.Year, today.Month));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View("Form", new BudgetFormViewModel { Categories = await AvailableCategoriesAsync(null, cancellationToken) });

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var budget = await budgets.GetAsync(id, cancellationToken);
        return View("Form", new BudgetFormViewModel
        {
            Id = budget.Id,
            CategoryId = budget.CategoryId,
            CategoryName = budget.CategoryName,
            MonthlyLimit = budget.MonthlyLimit,
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(BudgetFormViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                if (model.Id is { } id)
                {
                    await budgets.UpdateAsync(id, new UpdateBudgetRequest(model.MonthlyLimit!.Value), cancellationToken);
                    Flash("Budget updated.");
                }
                else
                {
                    await budgets.CreateAsync(new CreateBudgetRequest(model.CategoryId!.Value, model.MonthlyLimit!.Value), cancellationToken);
                    Flash("Budget created.");
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ValidationException or DomainException)
            {
                AddErrors(ex);
            }
            catch (ConflictException ex)
            {
                ModelState.AddModelError(nameof(model.CategoryId), ex.Message);
            }
        }

        model.Categories = await AvailableCategoriesAsync(model.CategoryId, cancellationToken);
        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await budgets.DeleteAsync(id, cancellationToken);
        Flash("Budget deleted.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Only categories without a budget yet can be picked for a new one.</summary>
    private async Task<IEnumerable<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> AvailableCategoriesAsync(
        int? selected, CancellationToken cancellationToken)
    {
        var taken = (await budgets.ListAsync(cancellationToken: cancellationToken)).Select(b => b.CategoryId).ToHashSet();
        var options = await CategoryOptionsAsync(categories, selected, cancellationToken);
        return options.Where(o => !taken.Contains(int.Parse(o.Value, System.Globalization.CultureInfo.InvariantCulture)));
    }
}
