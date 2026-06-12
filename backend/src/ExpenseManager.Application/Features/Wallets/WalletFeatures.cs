using AutoMapper;
using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Wallets;

// ===== Query =====
public record GetWalletsQuery : IRequest<List<WalletDto>>;

public class GetWalletsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<GetWalletsQuery, List<WalletDto>>
{
    public async Task<List<WalletDto>> Handle(GetWalletsQuery request, CancellationToken ct)
    {
        var wallets = await uow.Wallets.Query()
            .Where(w => w.UserId == currentUser.UserId)
            .OrderBy(w => w.CreatedDate)
            .ToListAsync(ct);

        var balances = await uow.Transactions.Query()
            .Where(t => t.UserId == currentUser.UserId)
            .GroupBy(t => t.WalletId)
            .Select(g => new
            {
                WalletId = g.Key,
                Net = g.Sum(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount)
            })
            .ToDictionaryAsync(x => x.WalletId, x => x.Net, ct);

        return wallets.Select(w =>
        {
            var dto = mapper.Map<WalletDto>(w);
            dto.CurrentBalance = w.InitialBalance + balances.GetValueOrDefault(w.Id);
            return dto;
        }).ToList();
    }
}

// ===== Create =====
public record CreateWalletCommand(string Name, WalletType Type, decimal InitialBalance, string Currency)
    : IRequest<WalletDto>;

public class CreateWalletCommandValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
    }
}

public class CreateWalletCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<CreateWalletCommand, WalletDto>
{
    public async Task<WalletDto> Handle(CreateWalletCommand request, CancellationToken ct)
    {
        var wallet = new Wallet
        {
            Name = request.Name.Trim(), Type = request.Type,
            InitialBalance = request.InitialBalance, Currency = request.Currency,
            UserId = currentUser.UserId
        };
        await uow.Wallets.AddAsync(wallet, ct);
        await uow.SaveChangesAsync(ct);

        var dto = mapper.Map<WalletDto>(wallet);
        dto.CurrentBalance = wallet.InitialBalance;
        return dto;
    }
}

// ===== Update =====
public record UpdateWalletCommand(Guid Id, string Name, WalletType Type, decimal InitialBalance, string Currency)
    : IRequest<WalletDto>;

public class UpdateWalletCommandValidator : AbstractValidator<UpdateWalletCommand>
{
    public UpdateWalletCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
    }
}

public class UpdateWalletCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<UpdateWalletCommand, WalletDto>
{
    public async Task<WalletDto> Handle(UpdateWalletCommand request, CancellationToken ct)
    {
        var wallet = await uow.Wallets.Query()
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy ví.");

        wallet.Name = request.Name.Trim();
        wallet.Type = request.Type;
        wallet.InitialBalance = request.InitialBalance;
        wallet.Currency = request.Currency;
        uow.Wallets.Update(wallet);
        await uow.SaveChangesAsync(ct);

        var net = await uow.Transactions.Query()
            .Where(t => t.WalletId == wallet.Id)
            .SumAsync(t => t.Type == TransactionType.Income ? t.Amount : -t.Amount, ct);

        var dto = mapper.Map<WalletDto>(wallet);
        dto.CurrentBalance = wallet.InitialBalance + net;
        return dto;
    }
}

// ===== Delete =====
public record DeleteWalletCommand(Guid Id) : IRequest;

public class DeleteWalletCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<DeleteWalletCommand>
{
    public async Task Handle(DeleteWalletCommand request, CancellationToken ct)
    {
        var wallet = await uow.Wallets.Query()
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy ví.");

        var hasTransactions = await uow.Transactions.Query().AnyAsync(t => t.WalletId == wallet.Id, ct);
        if (hasTransactions)
            throw new ConflictException("Ví đang có giao dịch, không thể xóa.");

        uow.Wallets.Remove(wallet);
        await uow.SaveChangesAsync(ct);
    }
}
