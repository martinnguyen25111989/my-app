using ExpenseManager.Domain.Entities;

namespace ExpenseManager.Application.Common.Interfaces;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<Category> Categories { get; }
    IRepository<Transaction> Transactions { get; }
    IRepository<Wallet> Wallets { get; }
    IRepository<Budget> Budgets { get; }
    IRepository<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
