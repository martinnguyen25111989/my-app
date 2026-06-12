using AutoMapper;
using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Budgets;

public static class BudgetAlertHelper
{
    public const decimal WarningThreshold = 80m;

    public static BudgetAlertDto? BuildAlert(Budget budget, string categoryName, decimal spent)
    {
        var percentage = budget.Amount > 0 ? Math.Round(spent / budget.Amount * 100, 1) : 0;
        if (percentage < WarningThreshold) return null;

        var exceeded = percentage >= 100;
        return new BudgetAlertDto
        {
            BudgetId = budget.Id,
            CategoryName = categoryName,
            Amount = budget.Amount,
            Spent = spent,
            Percentage = percentage,
            Level = exceeded ? "Exceeded" : "Warning",
            Message = exceeded
                ? $"Bạn đã vượt ngân sách \"{categoryName}\" ({percentage}%)!"
                : $"Bạn đã dùng {percentage}% ngân sách \"{categoryName}\"."
        };
    }
}

// ===== Query =====
public record GetBudgetsQuery(int Month, int Year) : IRequest<List<BudgetDto>>;

public class GetBudgetsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<GetBudgetsQuery, List<BudgetDto>>
{
    public async Task<List<BudgetDto>> Handle(GetBudgetsQuery request, CancellationToken ct)
    {
        var budgets = await uow.Budgets.Query()
            .Include(b => b.Category)
            .Where(b => b.UserId == currentUser.UserId && b.Month == request.Month && b.Year == request.Year)
            .ToListAsync(ct);

        var from = new DateOnly(request.Year, request.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var categoryIds = budgets.Select(b => b.CategoryId).ToList();

        var spentByCategory = await uow.Transactions.Query()
            .Where(t => t.UserId == currentUser.UserId
                        && t.Type == TransactionType.Expense
                        && t.TransactionDate >= from && t.TransactionDate <= to
                        && categoryIds.Contains(t.CategoryId))
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total, ct);

        return budgets.Select(b =>
        {
            var dto = mapper.Map<BudgetDto>(b);
            dto.Spent = spentByCategory.GetValueOrDefault(b.CategoryId);
            return dto;
        }).OrderByDescending(d => d.Percentage).ToList();
    }
}

// ===== Alerts =====
public record GetBudgetAlertsQuery(int Month, int Year) : IRequest<List<BudgetAlertDto>>;

public class GetBudgetAlertsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetBudgetAlertsQuery, List<BudgetAlertDto>>
{
    public async Task<List<BudgetAlertDto>> Handle(GetBudgetAlertsQuery request, CancellationToken ct)
    {
        var budgets = await uow.Budgets.Query()
            .Include(b => b.Category)
            .Where(b => b.UserId == currentUser.UserId && b.Month == request.Month && b.Year == request.Year)
            .ToListAsync(ct);
        if (budgets.Count == 0) return new List<BudgetAlertDto>();

        var from = new DateOnly(request.Year, request.Month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var categoryIds = budgets.Select(b => b.CategoryId).ToList();

        var spentByCategory = await uow.Transactions.Query()
            .Where(t => t.UserId == currentUser.UserId
                        && t.Type == TransactionType.Expense
                        && t.TransactionDate >= from && t.TransactionDate <= to
                        && categoryIds.Contains(t.CategoryId))
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Total, ct);

        return budgets
            .Select(b => BudgetAlertHelper.BuildAlert(b, b.Category.Name, spentByCategory.GetValueOrDefault(b.CategoryId)))
            .Where(a => a is not null)
            .Select(a => a!)
            .OrderByDescending(a => a.Percentage)
            .ToList();
    }
}

// ===== Create =====
public record CreateBudgetCommand(Guid CategoryId, decimal Amount, int Month, int Year) : IRequest<BudgetDto>;

public class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
        RuleFor(x => x.Year).InclusiveBetween(2000, 2100);
    }
}

public class CreateBudgetCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<CreateBudgetCommand, BudgetDto>
{
    public async Task<BudgetDto> Handle(CreateBudgetCommand request, CancellationToken ct)
    {
        var category = await uow.Categories.Query()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy danh mục.");
        if (category.Type != TransactionType.Expense)
            throw new BadRequestException("Chỉ có thể đặt ngân sách cho danh mục chi tiêu.");

        var exists = await uow.Budgets.Query().AnyAsync(b =>
            b.UserId == currentUser.UserId && b.CategoryId == request.CategoryId
            && b.Month == request.Month && b.Year == request.Year, ct);
        if (exists)
            throw new ConflictException("Ngân sách cho danh mục này trong tháng đã tồn tại.");

        var budget = new Budget
        {
            CategoryId = request.CategoryId, Amount = request.Amount,
            Month = request.Month, Year = request.Year, UserId = currentUser.UserId
        };
        await uow.Budgets.AddAsync(budget, ct);
        await uow.SaveChangesAsync(ct);

        budget.Category = category;
        return mapper.Map<BudgetDto>(budget);
    }
}

// ===== Update =====
public record UpdateBudgetCommand(Guid Id, decimal Amount) : IRequest<BudgetDto>;

public class UpdateBudgetCommandValidator : AbstractValidator<UpdateBudgetCommand>
{
    public UpdateBudgetCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public class UpdateBudgetCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<UpdateBudgetCommand, BudgetDto>
{
    public async Task<BudgetDto> Handle(UpdateBudgetCommand request, CancellationToken ct)
    {
        var budget = await uow.Budgets.Query()
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy ngân sách.");

        budget.Amount = request.Amount;
        uow.Budgets.Update(budget);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<BudgetDto>(budget);
    }
}

// ===== Delete =====
public record DeleteBudgetCommand(Guid Id) : IRequest;

public class DeleteBudgetCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<DeleteBudgetCommand>
{
    public async Task Handle(DeleteBudgetCommand request, CancellationToken ct)
    {
        var budget = await uow.Budgets.Query()
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy ngân sách.");

        uow.Budgets.Remove(budget);
        await uow.SaveChangesAsync(ct);
    }
}
