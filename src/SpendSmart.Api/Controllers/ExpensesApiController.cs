using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Common;
using SpendSmart.Application.Expenses;

namespace SpendSmart.Api.Controllers;

[Route("api/expenses")]
[Tags("Expenses")]
public sealed class ExpensesApiController(IExpenseService expenses) : ApiControllerBase
{
    /// <summary>
    /// Example: <c>GET /api/expenses?category=groceries&amp;from=2026-01-01&amp;sortBy=amount&amp;page=2</c>
    /// </summary>
    [HttpGet]
    [EndpointSummary("List expenses with filtering, sorting and paging")]
    [ProducesResponseType<PagedResult<ExpenseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<PagedResult<ExpenseDto>> List([FromQuery] ExpenseQuery query, CancellationToken cancellationToken) =>
        expenses.ListAsync(query, cancellationToken);

    [HttpGet("{id:int}")]
    [EndpointSummary("Get one expense")]
    [ProducesResponseType<ExpenseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ExpenseDto> Get(int id, CancellationToken cancellationToken) =>
        expenses.GetAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Create an expense")]
    [ProducesResponseType<ExpenseDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseDto>> Create(SaveExpenseRequest request, CancellationToken cancellationToken)
    {
        var created = await expenses.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update an expense")]
    [ProducesResponseType<ExpenseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ExpenseDto> Update(int id, SaveExpenseRequest request, CancellationToken cancellationToken) =>
        expenses.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete an expense")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await expenses.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
