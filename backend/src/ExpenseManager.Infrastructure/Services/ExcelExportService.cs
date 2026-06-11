using ClosedXML.Excel;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Enums;

namespace ExpenseManager.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] Export(ReportDto report)
    {
        using var workbook = new XLWorkbook();

        // Sheet 1: Tổng quan
        var summary = workbook.Worksheets.Add("Tổng quan");
        summary.Cell(1, 1).Value = "BÁO CÁO THU CHI";
        summary.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16);
        summary.Cell(2, 1).Value = $"Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}";
        summary.Cell(4, 1).Value = "Tổng thu";
        summary.Cell(4, 2).Value = report.TotalIncome;
        summary.Cell(5, 1).Value = "Tổng chi";
        summary.Cell(5, 2).Value = report.TotalExpense;
        summary.Cell(6, 1).Value = "Chênh lệch";
        summary.Cell(6, 2).Value = report.Balance;
        summary.Range(4, 2, 6, 2).Style.NumberFormat.Format = "#,##0";

        var row = 8;
        summary.Cell(row, 1).Value = "Chi tiêu theo danh mục";
        summary.Cell(row, 1).Style.Font.SetBold();
        row++;
        foreach (var c in report.ExpenseByCategory)
        {
            summary.Cell(row, 1).Value = c.CategoryName;
            summary.Cell(row, 2).Value = c.Total;
            summary.Cell(row, 2).Style.NumberFormat.Format = "#,##0";
            row++;
        }
        summary.Columns().AdjustToContents();

        // Sheet 2: Chi tiết giao dịch
        var detail = workbook.Worksheets.Add("Giao dịch");
        string[] headers = ["Ngày", "Loại", "Danh mục", "Ví", "Số tiền", "Ghi chú"];
        for (var i = 0; i < headers.Length; i++)
        {
            detail.Cell(1, i + 1).Value = headers[i];
            detail.Cell(1, i + 1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.LightGray);
        }

        var r = 2;
        foreach (var t in report.Transactions)
        {
            detail.Cell(r, 1).Value = t.TransactionDate.ToString("dd/MM/yyyy");
            detail.Cell(r, 2).Value = t.Type == TransactionType.Income ? "Thu" : "Chi";
            detail.Cell(r, 3).Value = t.CategoryName;
            detail.Cell(r, 4).Value = t.WalletName;
            detail.Cell(r, 5).Value = t.Type == TransactionType.Income ? t.Amount : -t.Amount;
            detail.Cell(r, 5).Style.NumberFormat.Format = "#,##0";
            detail.Cell(r, 6).Value = t.Note;
            r++;
        }
        detail.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
