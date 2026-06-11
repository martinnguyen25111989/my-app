using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Import;

/// <summary>
/// Import sao kê ngân hàng (CSV/Excel). Mỗi dòng được AI phân loại tự động vào danh mục phù hợp.
/// Số tiền dương = thu nhập, số tiền âm = chi tiêu.
/// </summary>
public record ImportBankStatementCommand(Stream FileStream, string FileName, Guid WalletId)
    : IRequest<ImportResultDto>;

public class ImportBankStatementCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IBankStatementParser parser,
    ITransactionClassifier classifier)
    : IRequestHandler<ImportBankStatementCommand, ImportResultDto>
{
    public async Task<ImportResultDto> Handle(ImportBankStatementCommand request, CancellationToken ct)
    {
        var wallet = await uow.Wallets.Query()
            .FirstOrDefaultAsync(w => w.Id == request.WalletId && w.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy ví.");

        List<BankStatementRow> rows;
        try
        {
            rows = parser.Parse(request.FileStream, request.FileName);
        }
        catch (Exception ex)
        {
            throw new BadRequestException($"Không đọc được file sao kê: {ex.Message}");
        }

        var categories = await uow.Categories.Query()
            .Where(c => c.UserId == currentUser.UserId)
            .ToListAsync(ct);
        var expenseCategories = categories.Where(c => c.Type == TransactionType.Expense)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Type = c.Type })
            .ToList();
        var fallbackExpense = categories.FirstOrDefault(c => c.Type == TransactionType.Expense)
            ?? throw new BadRequestException("Cần ít nhất một danh mục chi tiêu trước khi import.");
        var fallbackIncome = categories.FirstOrDefault(c => c.Type == TransactionType.Income)
            ?? throw new BadRequestException("Cần ít nhất một danh mục thu nhập trước khi import.");

        var result = new ImportResultDto();
        foreach (var row in rows)
        {
            if (row.Amount == 0)
            {
                result.Skipped++;
                continue;
            }

            var isIncome = row.Amount > 0;
            Guid categoryId;
            if (isIncome)
            {
                categoryId = fallbackIncome.Id;
            }
            else
            {
                var classification = await classifier.ClassifyAsync(row.Description, expenseCategories, ct);
                categoryId = classification.CategoryId ?? fallbackExpense.Id;
            }

            await uow.Transactions.AddAsync(new Transaction
            {
                Amount = Math.Abs(row.Amount),
                Type = isIncome ? TransactionType.Income : TransactionType.Expense,
                Note = row.Description,
                TransactionDate = row.Date,
                CategoryId = categoryId,
                WalletId = wallet.Id,
                UserId = currentUser.UserId
            }, ct);
            result.Imported++;
        }

        await uow.SaveChangesAsync(ct);
        return result;
    }
}
