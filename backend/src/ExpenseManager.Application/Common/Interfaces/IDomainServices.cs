using ExpenseManager.Application.Common.Models;

namespace ExpenseManager.Application.Common.Interfaces;

public interface IExcelExportService
{
    byte[] Export(ReportDto report);
}

public interface IPdfExportService
{
    byte[] Export(ReportDto report);
}

/// <summary>AI phân loại giao dịch: gợi ý danh mục dựa trên mô tả giao dịch.</summary>
public interface ITransactionClassifier
{
    Task<ClassificationResultDto> ClassifyAsync(
        string description, IReadOnlyList<CategoryDto> expenseCategories, CancellationToken ct = default);
}

public interface IBankStatementParser
{
    List<BankStatementRow> Parse(Stream stream, string fileName);
}
