using ExpenseManager.Domain.Common;

namespace ExpenseManager.Domain.Entities;

public class Budget : BaseEntity
{
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
