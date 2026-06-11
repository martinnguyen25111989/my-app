using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Transactions;
using ExpenseManager.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/transactions")]
public class TransactionsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TransactionDto>>> Get(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] TransactionType? type, [FromQuery] Guid? categoryId,
        [FromQuery] Guid? walletId, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20) =>
        Ok(await mediator.Send(new GetTransactionsQuery(
            fromDate, toDate, type, categoryId, walletId, search, page, pageSize)));

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create(CreateTransactionCommand command) =>
        Ok(await mediator.Send(command));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> Update(Guid id, UpdateTransactionCommand command) =>
        Ok(await mediator.Send(command with { Id = id }));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteTransactionCommand(id));
        return NoContent();
    }
}
