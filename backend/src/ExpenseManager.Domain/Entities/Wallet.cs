using ExpenseManager.Domain.Common;
using ExpenseManager.Domain.Enums;

namespace ExpenseManager.Domain.Entities;

public class Wallet : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public WalletType Type { get; set; }
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = "VND";

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
