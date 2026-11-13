using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Categories;
using SpendSmart.Application.RecurringExpenses;
using SpendSmart.Domain.Exceptions;
using SpendSmart.Web.ViewModels;

namespace SpendSmart.Web.Controllers;

public sealed class RecurringController(IRecurringExpenseService recurringExpenses, ICategoryService categories) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new RecurringListViewModel(await recurringExpenses.ListAsync(cancellationToken)));

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(new RecurringFormViewModel { Categories = await CategoryOptionsAsync(categories, null, cancellationToken) });

    [HttpPost]
    public async Task<IActionResult> Create(RecurringFormViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var created = await recurringExpenses.CreateAsync(
                    new CreateRecurringExpenseRequest(
                        model.Amount!.Value, model.Description, model.CategoryId!.Value, model.Frequency, model.StartDate),
                    cancellationToken);
                Flash($"Recurring expense saved. Next payment: {created.NextOccurrence:d MMM yyyy}.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ValidationException or DomainException)
            {
                AddErrors(ex);
            }
        }

        model.Categories = await CategoryOptionsAsync(categories, model.CategoryId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Pause(int id, CancellationToken cancellationToken)
    {
        await recurringExpenses.PauseAsync(id, cancellationToken);
        Flash("Paused. No new expenses will be created until you resume it.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Resume(int id, CancellationToken cancellationToken)
    {
        await recurringExpenses.ResumeAsync(id, cancellationToken);
        Flash("Resumed.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await recurringExpenses.DeleteAsync(id, cancellationToken);
        Flash("Recurring expense deleted. Expenses it already created were kept.");
        return RedirectToAction(nameof(Index));
    }
}
