using ExpenseManager.Domain.Enums;

namespace ExpenseManager.Application.Common.Models;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class WalletDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public WalletType Type { get; set; }
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal CurrentBalance { get; set; }
}

public class TransactionDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Note { get; set; }
    public DateOnly TransactionDate { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public Guid WalletId { get; set; }
    public string WalletName { get; set; } = string.Empty;
}

public class BudgetDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public decimal Spent { get; set; }
    public decimal Percentage => Amount > 0 ? Math.Round(Spent / Amount * 100, 1) : 0;
}

public class BudgetAlertDto
{
    public Guid BudgetId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Spent { get; set; }
    public decimal Percentage { get; set; }
    /// <summary>"Warning" khi >= 80%, "Exceeded" khi vượt 100%.</summary>
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class MonthlyChartPointDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class CategorySummaryDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class DashboardDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public List<MonthlyChartPointDto> MonthlyChart { get; set; } = new();
    public List<CategorySummaryDto> TopExpenseCategories { get; set; } = new();
    public List<WalletDto> Wallets { get; set; } = new();
    public List<BudgetAlertDto> BudgetAlerts { get; set; } = new();
}

public class ReportDto
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public List<CategorySummaryDto> IncomeByCategory { get; set; } = new();
    public List<CategorySummaryDto> ExpenseByCategory { get; set; } = new();
    public List<TransactionDto> Transactions { get; set; } = new();
}

public class ClassificationResultDto
{
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public double Confidence { get; set; }
}

public class ImportResultDto
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class BankStatementRow
{
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
