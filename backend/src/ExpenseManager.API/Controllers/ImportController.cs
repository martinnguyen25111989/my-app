using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Import;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/import")]
public class ImportController(IMediator mediator) : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    [HttpPost("bank-statement")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<ImportResultDto>> ImportBankStatement(
        IFormFile file, [FromForm] Guid walletId)
    {
        if (file is null || file.Length == 0)
            throw new BadRequestException("Vui lòng chọn file sao kê (.csv hoặc .xlsx).");

        await using var stream = file.OpenReadStream();
        return Ok(await mediator.Send(new ImportBankStatementCommand(stream, file.FileName, walletId)));
    }
}
