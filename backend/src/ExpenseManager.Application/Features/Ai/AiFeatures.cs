using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Ai;

// ===== AI phân loại giao dịch =====
public record ClassifyTransactionQuery(string Description) : IRequest<ClassificationResultDto>;

public class ClassifyTransactionQueryValidator : AbstractValidator<ClassifyTransactionQuery>
{
    public ClassifyTransactionQueryValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}

public class ClassifyTransactionQueryHandler(
    IUnitOfWork uow, ICurrentUserService currentUser, ITransactionClassifier classifier)
    : IRequestHandler<ClassifyTransactionQuery, ClassificationResultDto>
{
    public async Task<ClassificationResultDto> Handle(ClassifyTransactionQuery request, CancellationToken ct)
    {
        var categories = await uow.Categories.Query()
            .Where(c => c.UserId == currentUser.UserId && c.Type == TransactionType.Expense)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Type = c.Type })
            .ToListAsync(ct);

        return await classifier.ClassifyAsync(request.Description, categories, ct);
    }
}

// ===== Thống kê chi tiêu bằng AI =====
public record GetSpendingInsightsQuery(int Month, int Year) : IRequest<List<string>>;

public class GetSpendingInsightsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetSpendingInsightsQuery, List<string>>
{
    public async Task<List<string>> Handle(GetSpendingInsightsQuery request, CancellationToken ct)
    {
        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var prevStart = monthStart.AddMonths(-1);
        var prevEnd = monthStart.AddDays(-1);

        var data = await uow.Transactions.Query()
            .Where(t => t.UserId == currentUser.UserId
                        && t.TransactionDate >= prevStart && t.TransactionDate <= monthEnd)
            .Select(t => new
            {
                t.Type, t.Amount, t.TransactionDate,
                CategoryName = t.Category.Name
            })
            .ToListAsync(ct);

        var insights = new List<string>();
        var current = data.Where(t => t.TransactionDate >= monthStart).ToList();
        var previous = data.Where(t => t.TransactionDate < monthStart).ToList();

        var currentExpense = current.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var previousExpense = previous.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var currentIncome = current.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);

        if (current.Count == 0)
        {
            insights.Add("Chưa có giao dịch nào trong tháng này. Hãy bắt đầu ghi chép chi tiêu!");
            return insights;
        }

        // So sánh với tháng trước
        if (previousExpense > 0)
        {
            var change = Math.Round((currentExpense - previousExpense) / previousExpense * 100, 1);
            insights.Add(change > 0
                ? $"Chi tiêu tháng này tăng {change}% so với tháng trước ({currentExpense:N0} so với {previousExpense:N0})."
                : $"Tuyệt vời! Chi tiêu tháng này giảm {Math.Abs(change)}% so với tháng trước.");
        }

        // Danh mục chi nhiều nhất
        var topCategory = current.Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.CategoryName)
            .Select(g => new { Name = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();
        if (topCategory is not null && currentExpense > 0)
        {
            var pct = Math.Round(topCategory.Total / currentExpense * 100, 1);
            insights.Add($"\"{topCategory.Name}\" chiếm {pct}% tổng chi tiêu tháng này ({topCategory.Total:N0}).");
        }

        // Tỷ lệ tiết kiệm
        if (currentIncome > 0)
        {
            var savingRate = Math.Round((currentIncome - currentExpense) / currentIncome * 100, 1);
            insights.Add(savingRate >= 20
                ? $"Bạn đang tiết kiệm {savingRate}% thu nhập — duy trì tốt nhé!"
                : savingRate >= 0
                    ? $"Tỷ lệ tiết kiệm hiện tại là {savingRate}%. Mục tiêu khuyến nghị là 20% thu nhập."
                    : "Chi tiêu tháng này đã vượt thu nhập. Hãy xem lại các khoản chi lớn.");
        }

        // Chi tiêu trung bình theo ngày
        var daysElapsed = Math.Max(1, Math.Min(DateTime.UtcNow.Day,
            monthEnd.Day));
        var dailyAvg = currentExpense / daysElapsed;
        insights.Add($"Trung bình bạn chi {dailyAvg:N0}/ngày trong tháng này.");

        // Ngày chi nhiều bất thường
        var bigDay = current.Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.TransactionDate)
            .Select(g => new { Date = g.Key, Total = g.Sum(t => t.Amount) })
            .OrderByDescending(x => x.Total)
            .FirstOrDefault();
        if (bigDay is not null && dailyAvg > 0 && bigDay.Total > dailyAvg * 3)
            insights.Add($"Ngày {bigDay.Date:dd/MM} bạn chi {bigDay.Total:N0} — cao gấp {Math.Round(bigDay.Total / dailyAvg, 1)} lần mức trung bình.");

        return insights;
    }
}
