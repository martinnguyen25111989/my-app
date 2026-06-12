using AutoMapper;
using AutoMapper.QueryableExtensions;
using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Reports;

// ===== Báo cáo theo khoảng thời gian =====
public record GetReportQuery(DateOnly FromDate, DateOnly ToDate) : IRequest<ReportDto>;

public class GetReportQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<GetReportQuery, ReportDto>
{
    public async Task<ReportDto> Handle(GetReportQuery request, CancellationToken ct)
    {
        if (request.FromDate > request.ToDate)
            throw new BadRequestException("Khoảng thời gian không hợp lệ.");

        var query = uow.Transactions.Query()
            .Where(t => t.UserId == currentUser.UserId
                        && t.TransactionDate >= request.FromDate
                        && t.TransactionDate <= request.ToDate);

        var byCategory = await query
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Color, t.Type })
            .Select(g => new
            {
                g.Key.CategoryId, g.Key.Name, g.Key.Color, g.Key.Type,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(ct);

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedDate)
            .ProjectTo<TransactionDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        var totalIncome = byCategory.Where(x => x.Type == TransactionType.Income).Sum(x => x.Total);
        var totalExpense = byCategory.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Total);

        return new ReportDto
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = totalIncome - totalExpense,
            IncomeByCategory = byCategory.Where(x => x.Type == TransactionType.Income)
                .Select(x => new CategorySummaryDto
                {
                    CategoryId = x.CategoryId, CategoryName = x.Name, Color = x.Color, Total = x.Total
                }).OrderByDescending(x => x.Total).ToList(),
            ExpenseByCategory = byCategory.Where(x => x.Type == TransactionType.Expense)
                .Select(x => new CategorySummaryDto
                {
                    CategoryId = x.CategoryId, CategoryName = x.Name, Color = x.Color, Total = x.Total
                }).OrderByDescending(x => x.Total).ToList(),
            Transactions = transactions
        };
    }
}

// ===== Xuất Excel / PDF =====
public record ExportReportQuery(DateOnly FromDate, DateOnly ToDate, string Format)
    : IRequest<ExportFileResult>;

public record ExportFileResult(byte[] Content, string ContentType, string FileName);

public class ExportReportQueryHandler(
    IMediator mediator, IExcelExportService excelService, IPdfExportService pdfService)
    : IRequestHandler<ExportReportQuery, ExportFileResult>
{
    public async Task<ExportFileResult> Handle(ExportReportQuery request, CancellationToken ct)
    {
        var report = await mediator.Send(new GetReportQuery(request.FromDate, request.ToDate), ct);
        var suffix = $"{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}";

        return request.Format.ToLowerInvariant() switch
        {
            "excel" => new ExportFileResult(
                excelService.Export(report),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"bao-cao-thu-chi_{suffix}.xlsx"),
            "pdf" => new ExportFileResult(
                pdfService.Export(report),
                "application/pdf",
                $"bao-cao-thu-chi_{suffix}.pdf"),
            _ => throw new BadRequestException("Định dạng xuất không hợp lệ (excel | pdf).")
        };
    }
}
