using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Application.Features.Budgets;
using ExpenseManager.Application.Features.Wallets;
using ExpenseManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Dashboard;

public record GetDashboardQuery(int Month, int Year) : IRequest<DashboardDto>;

public class GetDashboardQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMediator mediator)
    : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken ct)
    {
        var monthStart = new DateOnly(request.Year, request.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var monthTotals = await uow.Transactions.Query()
            .Where(t => t.UserId == currentUser.UserId
                        && t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        var totalIncome = monthTotals.FirstOrDefault(x => x.Type == TransactionType.Income)?.Total ?? 0;
        var totalExpense = monthTotals.FirstOrDefault(x => x.Type == TransactionType.Expense)?.Total ?? 0;

        // Số dư hiện tại = tổng số dư của tất cả các ví
        var wallets = await mediator.Send(new GetWalletsQuery(), ct);
        var balance = wallets.Sum(w => w.CurrentBalance);

        // Biểu đồ thu chi 6 tháng gần nhất
        var chartStart = monthStart.AddMonths(-5);
        var chartData = await uow.Transactions.Query()
            .Where(t => t.UserId == currentUser.UserId
                        && t.TransactionDate >= chartStart && t.TransactionDate <= monthEnd)
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month, t.Type })
            .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Type, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);

        var monthlyChart = new List<MonthlyChartPointDto>();
        for (var d = chartStart; d <= monthStart; d = d.AddMonths(1))
        {
            monthlyChart.Add(new MonthlyChartPointDto
            {
                Month = d.Month,
                Year = d.Year,
                Income = chartData.Where(x => x.Year == d.Year && x.Month == d.Month && x.Type == TransactionType.Income).Sum(x => x.Total),
                Expense = chartData.Where(x => x.Year == d.Year && x.Month == d.Month && x.Type == TransactionType.Expense).Sum(x => x.Total)
            });
        }

        // Top 5 danh mục chi tiêu trong tháng
        var topCategories = await uow.Transactions.Query()
            .Where(t => t.UserId == currentUser.UserId
                        && t.Type == TransactionType.Expense
                        && t.TransactionDate >= monthStart && t.TransactionDate <= monthEnd)
            .GroupBy(t => new { t.CategoryId, t.Category.Name, t.Category.Color })
            .Select(g => new CategorySummaryDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Color = g.Key.Color,
                Total = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToListAsync(ct);

        var alerts = await mediator.Send(new GetBudgetAlertsQuery(request.Month, request.Year), ct);

        return new DashboardDto
        {
            TotalIncome = totalIncome,
            TotalExpense = totalExpense,
            Balance = balance,
            MonthlyChart = monthlyChart,
            TopExpenseCategories = topCategories,
            Wallets = wallets,
            BudgetAlerts = alerts
        };
    }
}
