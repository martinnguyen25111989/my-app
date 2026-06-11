using AutoMapper;
using AutoMapper.QueryableExtensions;
using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Transactions;

// ===== Query (filter + pagination) =====
public record GetTransactionsQuery(
    DateOnly? FromDate, DateOnly? ToDate, TransactionType? Type,
    Guid? CategoryId, Guid? WalletId, string? Search,
    int Page = 1, int PageSize = 20) : IRequest<PagedResult<TransactionDto>>;

public class GetTransactionsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    public async Task<PagedResult<TransactionDto>> Handle(GetTransactionsQuery request, CancellationToken ct)
    {
        var query = uow.Transactions.Query().Where(t => t.UserId == currentUser.UserId);

        if (request.FromDate.HasValue) query = query.Where(t => t.TransactionDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(t => t.TransactionDate <= request.ToDate.Value);
        if (request.Type.HasValue) query = query.Where(t => t.Type == request.Type.Value);
        if (request.CategoryId.HasValue) query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        if (request.WalletId.HasValue) query = query.Where(t => t.WalletId == request.WalletId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(t => t.Note != null && t.Note.ToLower().Contains(search));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.TransactionDate).ThenByDescending(t => t.CreatedDate)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ProjectTo<TransactionDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<TransactionDto>
        {
            Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }
}

// ===== Create =====
public record CreateTransactionCommand(
    decimal Amount, TransactionType Type, string? Note, DateOnly TransactionDate,
    Guid CategoryId, Guid WalletId) : IRequest<TransactionDto>;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.WalletId).NotEmpty();
    }
}

public class CreateTransactionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(CreateTransactionCommand request, CancellationToken ct)
    {
        var category = await uow.Categories.Query()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy danh mục.");
        if (category.Type != request.Type)
            throw new BadRequestException("Loại giao dịch không khớp với loại danh mục.");

        var wallet = await uow.Wallets.Query()
            .FirstOrDefaultAsync(w => w.Id == request.WalletId && w.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy ví.");

        var transaction = new Transaction
        {
            Amount = request.Amount, Type = request.Type, Note = request.Note?.Trim(),
            TransactionDate = request.TransactionDate,
            CategoryId = category.Id, WalletId = wallet.Id, UserId = currentUser.UserId
        };
        await uow.Transactions.AddAsync(transaction, ct);
        await uow.SaveChangesAsync(ct);

        transaction.Category = category;
        transaction.Wallet = wallet;
        return mapper.Map<TransactionDto>(transaction);
    }
}

// ===== Update =====
public record UpdateTransactionCommand(
    Guid Id, decimal Amount, TransactionType Type, string? Note, DateOnly TransactionDate,
    Guid CategoryId, Guid WalletId) : IRequest<TransactionDto>;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.WalletId).NotEmpty();
    }
}

public class UpdateTransactionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(UpdateTransactionCommand request, CancellationToken ct)
    {
        var transaction = await uow.Transactions.Query()
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy giao dịch.");

        var category = await uow.Categories.Query()
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy danh mục.");
        if (category.Type != request.Type)
            throw new BadRequestException("Loại giao dịch không khớp với loại danh mục.");

        var wallet = await uow.Wallets.Query()
            .FirstOrDefaultAsync(w => w.Id == request.WalletId && w.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy ví.");

        transaction.Amount = request.Amount;
        transaction.Type = request.Type;
        transaction.Note = request.Note?.Trim();
        transaction.TransactionDate = request.TransactionDate;
        transaction.CategoryId = category.Id;
        transaction.WalletId = wallet.Id;
        uow.Transactions.Update(transaction);
        await uow.SaveChangesAsync(ct);

        transaction.Category = category;
        transaction.Wallet = wallet;
        return mapper.Map<TransactionDto>(transaction);
    }
}

// ===== Delete (soft delete) =====
public record DeleteTransactionCommand(Guid Id) : IRequest;

public class DeleteTransactionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(DeleteTransactionCommand request, CancellationToken ct)
    {
        var transaction = await uow.Transactions.Query()
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy giao dịch.");

        uow.Transactions.Remove(transaction);
        await uow.SaveChangesAsync(ct);
    }
}
