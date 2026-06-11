using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseManager.API.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
public class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ReportDto>> Get([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate) =>
        Ok(await mediator.Send(new GetReportQuery(fromDate, toDate)));

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
    {
        var file = await mediator.Send(new ExportReportQuery(fromDate, toDate, "excel"));
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
    {
        var file = await mediator.Send(new ExportReportQuery(fromDate, toDate, "pdf"));
        return File(file.Content, file.ContentType, file.FileName);
    }
}
