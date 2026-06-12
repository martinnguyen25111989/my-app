using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ExpenseManager.Infrastructure.Services;

public class PdfExportService : IPdfExportService
{
    public byte[] Export(ReportDto report)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("BÁO CÁO THU CHI").FontSize(18).Bold();
                    col.Item().Text($"Từ {report.FromDate:dd/MM/yyyy} đến {report.ToDate:dd/MM/yyyy}")
                        .FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(10);
                });

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("Tổng thu").FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{report.TotalIncome:N0}").FontSize(14).Bold()
                                .FontColor(Colors.Green.Darken2);
                        });
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("Tổng chi").FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{report.TotalExpense:N0}").FontSize(14).Bold()
                                .FontColor(Colors.Red.Darken2);
                        });
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(c =>
                        {
                            c.Item().Text("Chênh lệch").FontColor(Colors.Grey.Darken1);
                            c.Item().Text($"{report.Balance:N0}").FontSize(14).Bold();
                        });
                    });

                    col.Item().PaddingTop(20).Text("Chi tiêu theo danh mục").FontSize(12).Bold();
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });
                        foreach (var cat in report.ExpenseByCategory)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .PaddingVertical(4).Text(cat.CategoryName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .PaddingVertical(4).AlignRight().Text($"{cat.Total:N0}");
                        }
                    });

                    col.Item().PaddingTop(20).Text("Chi tiết giao dịch").FontSize(12).Bold();
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(70);
                            c.ConstantColumn(40);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Ngày", "Loại", "Danh mục", "Số tiền", "Ghi chú" })
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(h).Bold();
                        });

                        foreach (var t in report.Transactions)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(4).Text(t.TransactionDate.ToString("dd/MM/yyyy"));
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(4).Text(t.Type == TransactionType.Income ? "Thu" : "Chi");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(4).Text(t.CategoryName);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(4).AlignRight().Text($"{t.Amount:N0}");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
                                .Padding(4).Text(t.Note ?? "");
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
