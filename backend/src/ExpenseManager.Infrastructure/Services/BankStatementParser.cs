using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;

namespace ExpenseManager.Infrastructure.Services;

/// <summary>
/// Đọc sao kê ngân hàng CSV/Excel. Định dạng kỳ vọng (3 cột, có dòng header):
/// Date (dd/MM/yyyy hoặc yyyy-MM-dd), Description, Amount (dương = thu, âm = chi).
/// </summary>
public class BankStatementParser : IBankStatementParser
{
    private static readonly string[] DateFormats =
        ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "dd-MM-yyyy"];

    public List<BankStatementRow> Parse(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".csv" => ParseCsv(stream),
            ".xlsx" or ".xls" => ParseExcel(stream),
            _ => throw new InvalidOperationException("Chỉ hỗ trợ file .csv hoặc .xlsx")
        };
    }

    private static List<BankStatementRow> ParseCsv(Stream stream)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, config);

        var rows = new List<BankStatementRow>();
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            var date = ParseDate(csv.GetField(0));
            var description = csv.GetField(1) ?? string.Empty;
            var amount = ParseAmount(csv.GetField(2));
            if (date is null || amount is null) continue;
            rows.Add(new BankStatementRow { Date = date.Value, Description = description, Amount = amount.Value });
        }
        return rows;
    }

    private static List<BankStatementRow> ParseExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var rows = new List<BankStatementRow>();

        foreach (var row in sheet.RowsUsed().Skip(1)) // bỏ qua header
        {
            DateOnly? date = row.Cell(1).DataType == XLDataType.DateTime
                ? DateOnly.FromDateTime(row.Cell(1).GetDateTime())
                : ParseDate(row.Cell(1).GetString());
            var description = row.Cell(2).GetString();
            decimal? amount = row.Cell(3).DataType == XLDataType.Number
                ? (decimal)row.Cell(3).GetDouble()
                : ParseAmount(row.Cell(3).GetString());

            if (date is null || amount is null) continue;
            rows.Add(new BankStatementRow { Date = date.Value, Description = description, Amount = amount.Value });
        }
        return rows;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        foreach (var format in DateFormats)
        {
            if (DateOnly.TryParseExact(value.Trim(), format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
                return date;
        }
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var fallback) ? fallback : null;
    }

    private static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Replace(",", "").Replace(" ", "").Replace("₫", "").Replace("VND", "");
        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }
}
