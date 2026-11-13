using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.RecurringExpenses;

namespace SpendSmart.Api.Controllers;

[Route("api/recurring-expenses")]
[Tags("Recurring expenses")]
public sealed class RecurringExpensesApiController(IRecurringExpenseService recurringExpenses) : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("List recurring expense templates")]
    [ProducesResponseType<IReadOnlyList<RecurringExpenseDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<RecurringExpenseDto>> List(CancellationToken cancellationToken) =>
        recurringExpenses.ListAsync(cancellationToken);

    /// <summary>Occurrences already due are created immediately; the rest are created by the background job.</summary>
    [HttpPost]
    [EndpointSummary("Create a recurring expense (rent, subscriptions…)")]
    [ProducesResponseType<RecurringExpenseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringExpenseDto>> Create(CreateRecurringExpenseRequest request, CancellationToken cancellationToken)
    {
        var created = await recurringExpenses.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPost("{id:int}/pause")]
    [EndpointSummary("Stop generating expenses from a template")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pause(int id, CancellationToken cancellationToken)
    {
        await recurringExpenses.PauseAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/resume")]
    [EndpointSummary("Resume a paused template (missed occurrences are skipped)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resume(int id, CancellationToken cancellationToken)
    {
        await recurringExpenses.ResumeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete a template (expenses already generated are kept)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await recurringExpenses.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
