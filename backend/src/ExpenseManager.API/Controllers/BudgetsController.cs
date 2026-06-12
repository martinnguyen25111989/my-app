using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Budgets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/budgets")]
public class BudgetsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<BudgetDto>>> Get([FromQuery] int month, [FromQuery] int year) =>
        Ok(await mediator.Send(new GetBudgetsQuery(month, year)));

    [HttpGet("alerts")]
    public async Task<ActionResult<List<BudgetAlertDto>>> GetAlerts([FromQuery] int month, [FromQuery] int year) =>
        Ok(await mediator.Send(new GetBudgetAlertsQuery(month, year)));

    [HttpPost]
    public async Task<ActionResult<BudgetDto>> Create(CreateBudgetCommand command) =>
        Ok(await mediator.Send(command));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BudgetDto>> Update(Guid id, UpdateBudgetCommand command) =>
        Ok(await mediator.Send(command with { Id = id }));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteBudgetCommand(id));
        return NoContent();
    }
}
