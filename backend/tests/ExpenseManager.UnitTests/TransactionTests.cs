using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Features.Transactions;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseManager.UnitTests;

public class TransactionTests : TestBase
{
    private Category _expenseCategory = null!;
    private Category _incomeCategory = null!;
    private Wallet _wallet = null!;

    private async Task SeedAsync()
    {
        await SeedUserAsync();
        _expenseCategory = new Category { Name = "Ăn uống", Type = TransactionType.Expense, UserId = UserId };
        _incomeCategory = new Category { Name = "Lương", Type = TransactionType.Income, UserId = UserId };
        _wallet = new Wallet { Name = "Tiền mặt", Type = WalletType.Cash, InitialBalance = 1000, UserId = UserId };
        Context.AddRange(_expenseCategory, _incomeCategory, _wallet);
        await Context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateTransaction_ShouldPersist_WhenValid()
    {
        await SeedAsync();
        var handler = new CreateTransactionCommandHandler(Uow, CurrentUser.Object, Mapper);

        var result = await handler.Handle(new CreateTransactionCommand(
            50000, TransactionType.Expense, "Cơm trưa", new DateOnly(2026, 6, 10),
            _expenseCategory.Id, _wallet.Id), CancellationToken.None);

        result.Amount.Should().Be(50000);
        result.CategoryName.Should().Be("Ăn uống");
        result.WalletName.Should().Be("Tiền mặt");
        (await Context.Transactions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateTransaction_ShouldThrow_WhenTypeMismatchesCategory()
    {
        await SeedAsync();
        var handler = new CreateTransactionCommandHandler(Uow, CurrentUser.Object, Mapper);

        var act = () => handler.Handle(new CreateTransactionCommand(
            50000, TransactionType.Income, null, new DateOnly(2026, 6, 10),
            _expenseCategory.Id, _wallet.Id), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetTransactions_ShouldFilterByDateRangeAndType()
    {
        await SeedAsync();
        Context.Transactions.AddRange(
            new Transaction { Amount = 100, Type = TransactionType.Expense, TransactionDate = new DateOnly(2026, 6, 1), CategoryId = _expenseCategory.Id, WalletId = _wallet.Id, UserId = UserId },
            new Transaction { Amount = 200, Type = TransactionType.Expense, TransactionDate = new DateOnly(2026, 5, 1), CategoryId = _expenseCategory.Id, WalletId = _wallet.Id, UserId = UserId },
            new Transaction { Amount = 999, Type = TransactionType.Income, TransactionDate = new DateOnly(2026, 6, 5), CategoryId = _incomeCategory.Id, WalletId = _wallet.Id, UserId = UserId });
        await Context.SaveChangesAsync();

        var handler = new GetTransactionsQueryHandler(Uow, CurrentUser.Object, Mapper);
        var result = await handler.Handle(new GetTransactionsQuery(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30),
            TransactionType.Expense, null, null, null), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Single().Amount.Should().Be(100);
    }
}
