using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Wallets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wallets")]
public class WalletsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<WalletDto>>> Get() =>
        Ok(await mediator.Send(new GetWalletsQuery()));

    [HttpPost]
    public async Task<ActionResult<WalletDto>> Create(CreateWalletCommand command) =>
        Ok(await mediator.Send(command));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WalletDto>> Update(Guid id, UpdateWalletCommand command) =>
        Ok(await mediator.Send(command with { Id = id }));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteWalletCommand(id));
        return NoContent();
    }
}
