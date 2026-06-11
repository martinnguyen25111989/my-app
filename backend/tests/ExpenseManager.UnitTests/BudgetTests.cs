using ExpenseManager.Application.Features.Budgets;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ExpenseManager.UnitTests;

public class BudgetTests : TestBase
{
    [Theory]
    [InlineData(1000, 500, null, null)]            // 50% — không cảnh báo
    [InlineData(1000, 800, "Warning", 80)]         // 80% — cảnh báo
    [InlineData(1000, 1200, "Exceeded", 120)]      // 120% — vượt ngân sách
    public void BuildAlert_ShouldReturnCorrectLevel(
        decimal amount, decimal spent, string? expectedLevel, double? expectedPercentage)
    {
        var budget = new Budget { Amount = amount, Month = 6, Year = 2026 };

        var alert = BudgetAlertHelper.BuildAlert(budget, "Ăn uống", spent);

        if (expectedLevel is null)
        {
            alert.Should().BeNull();
        }
        else
        {
            alert.Should().NotBeNull();
            alert!.Level.Should().Be(expectedLevel);
            alert.Percentage.Should().Be((decimal)expectedPercentage!.Value);
        }
    }

    [Fact]
    public async Task GetBudgets_ShouldComputeSpentAmount()
    {
        await SeedUserAsync();
        var category = new Category { Name = "Ăn uống", Type = TransactionType.Expense, UserId = UserId };
        var wallet = new Wallet { Name = "Cash", Type = WalletType.Cash, UserId = UserId };
        var budget = new Budget { Amount = 1000, Month = 6, Year = 2026, CategoryId = category.Id, UserId = UserId };
        Context.AddRange(category, wallet, budget,
            new Transaction { Amount = 300, Type = TransactionType.Expense, TransactionDate = new DateOnly(2026, 6, 5), CategoryId = category.Id, WalletId = wallet.Id, UserId = UserId },
            new Transaction { Amount = 150, Type = TransactionType.Expense, TransactionDate = new DateOnly(2026, 6, 20), CategoryId = category.Id, WalletId = wallet.Id, UserId = UserId },
            // Tháng khác — không tính
            new Transaction { Amount = 999, Type = TransactionType.Expense, TransactionDate = new DateOnly(2026, 5, 5), CategoryId = category.Id, WalletId = wallet.Id, UserId = UserId });
        await Context.SaveChangesAsync();

        var handler = new GetBudgetsQueryHandler(Uow, CurrentUser.Object, Mapper);
        var result = await handler.Handle(new GetBudgetsQuery(6, 2026), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Spent.Should().Be(450);
        result[0].Percentage.Should().Be(45);
    }
}
