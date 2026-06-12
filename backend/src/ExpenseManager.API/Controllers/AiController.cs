using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Ai;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
public class AiController(IMediator mediator) : ControllerBase
{
    [HttpPost("classify")]
    public async Task<ActionResult<ClassificationResultDto>> Classify(ClassifyTransactionQuery query) =>
        Ok(await mediator.Send(query));

    [HttpGet("insights")]
    public async Task<ActionResult<List<string>>> GetInsights([FromQuery] int? month, [FromQuery] int? year)
    {
        var now = DateTime.UtcNow;
        return Ok(await mediator.Send(new GetSpendingInsightsQuery(month ?? now.Month, year ?? now.Year)));
    }
}
