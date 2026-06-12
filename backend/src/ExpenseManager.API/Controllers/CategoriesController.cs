using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Categories;
using ExpenseManager.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CategoryDto>>> Get(
        [FromQuery] string? search, [FromQuery] TransactionType? type,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
        Ok(await mediator.Send(new GetCategoriesQuery(search, type, page, pageSize)));

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryCommand command) =>
        Ok(await mediator.Send(command));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryCommand command) =>
        Ok(await mediator.Send(command with { Id = id }));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}
