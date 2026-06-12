using ExpenseManager.Domain.Common;
using ExpenseManager.Domain.Enums;

namespace ExpenseManager.Domain.Entities;

public class Transaction : BaseEntity
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public string? Note { get; set; }
    public DateOnly TransactionDate { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
