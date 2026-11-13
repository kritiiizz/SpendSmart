using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Categories;
using SpendSmart.Application.Expenses;
using SpendSmart.Domain.Exceptions;
using SpendSmart.Web.ViewModels;

namespace SpendSmart.Web.Controllers;

public sealed class ExpensesController(IExpenseService expenses, ICategoryService categories) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ExpenseQuery query, CancellationToken cancellationToken)
    {
        // Silently fix bad paging input from hand-edited URLs instead of showing an error page.
        query = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, ExpenseQuery.MaxPageSize),
        };

        try
        {
            var result = await expenses.ListAsync(query, cancellationToken);
            return View(new ExpenseListViewModel
            {
                Result = result,
                Query = query,
                Categories = await categories.ListAsync(cancellationToken),
            });
        }
        catch (ValidationException ex)
        {
            FlashError(string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)));
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View("Form", new ExpenseFormViewModel
        {
            Categories = await CategoryOptionsAsync(categories, null, cancellationToken),
        });

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var expense = await expenses.GetAsync(id, cancellationToken);
        return View("Form", new ExpenseFormViewModel
        {
            Id = expense.Id,
            Amount = expense.Amount,
            Description = expense.Description,
            Date = expense.Date,
            CategoryId = expense.CategoryId,
            Categories = await CategoryOptionsAsync(categories, expense.CategoryId, cancellationToken),
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(ExpenseFormViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var request = new SaveExpenseRequest(model.Amount!.Value, model.Description, model.Date, model.CategoryId!.Value);
            try
            {
                if (model.Id is { } id)
                {
                    await expenses.UpdateAsync(id, request, cancellationToken);
                    Flash("Expense updated.");
                }
                else
                {
                    await expenses.CreateAsync(request, cancellationToken);
                    Flash("Expense added.");
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex) when (ex is ValidationException or DomainException)
            {
                AddErrors(ex);
            }
        }

        model.Categories = await CategoryOptionsAsync(categories, model.CategoryId, cancellationToken);
        return View("Form", model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        await expenses.DeleteAsync(id, cancellationToken);
        Flash("Expense deleted.");
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }
}
