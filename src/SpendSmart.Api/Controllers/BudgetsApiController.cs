using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Budgets;

namespace SpendSmart.Api.Controllers;

[Route("api/budgets")]
[Tags("Budgets")]
public sealed class BudgetsApiController(IBudgetService budgets) : ApiControllerBase
{
    /// <summary>Each budget includes spending for the month and a status: OnTrack, Warning (80%+) or Exceeded.</summary>
    [HttpGet]
    [EndpointSummary("List budgets with spending for a month (defaults to the current month)")]
    [ProducesResponseType<IReadOnlyList<BudgetDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<BudgetDto>> List([FromQuery] int? year, [FromQuery] int? month, CancellationToken cancellationToken) =>
        budgets.ListAsync(year, month, cancellationToken);

    [HttpGet("{id:int}")]
    [EndpointSummary("Get one budget with this month's spending")]
    [ProducesResponseType<BudgetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<BudgetDto> Get(int id, CancellationToken cancellationToken) =>
        budgets.GetAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Set a monthly limit for a category")]
    [ProducesResponseType<BudgetDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BudgetDto>> Create(CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var created = await budgets.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Change a budget's monthly limit")]
    [ProducesResponseType<BudgetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<BudgetDto> Update(int id, UpdateBudgetRequest request, CancellationToken cancellationToken) =>
        budgets.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete a budget")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await budgets.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
