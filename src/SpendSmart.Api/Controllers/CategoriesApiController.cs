using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendSmart.Application.Categories;

namespace SpendSmart.Api.Controllers;

[Route("api/categories")]
[Tags("Categories")]
public sealed class CategoriesApiController(ICategoryService categories) : ApiControllerBase
{
    [HttpGet]
    [EndpointSummary("List your categories")]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<CategoryDto>> List(CancellationToken cancellationToken) =>
        categories.ListAsync(cancellationToken);

    [HttpGet("{id:int}")]
    [EndpointSummary("Get one category")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CategoryDto> Get(int id, CancellationToken cancellationToken) =>
        categories.GetAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Create a category")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> Create(SaveCategoryRequest request, CancellationToken cancellationToken)
    {
        var created = await categories.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Rename or recolour a category")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<CategoryDto> Update(int id, SaveCategoryRequest request, CancellationToken cancellationToken) =>
        categories.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    [EndpointSummary("Delete an unused category")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await categories.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
