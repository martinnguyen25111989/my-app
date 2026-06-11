using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] int? month, [FromQuery] int? year)
    {
        var now = DateTime.UtcNow;
        return Ok(await mediator.Send(new GetDashboardQuery(month ?? now.Month, year ?? now.Year)));
    }
}
