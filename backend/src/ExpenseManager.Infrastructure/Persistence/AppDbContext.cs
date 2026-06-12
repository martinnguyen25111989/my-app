using ExpenseManager.Domain.Common;
using ExpenseManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
        });

        builder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Icon).HasMaxLength(50);
            e.Property(x => x.Color).HasMaxLength(7);
            e.HasOne(x => x.User).WithMany(u => u.Categories)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId);
        });

        builder.Entity<Wallet>(e =>
        {
            e.ToTable("Wallets");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(10);
            e.Property(x => x.InitialBalance).HasPrecision(18, 2);
            e.HasOne(x => x.User).WithMany(u => u.Wallets)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.UserId);
        });

        builder.Entity<Transaction>(e =>
        {
            e.ToTable("Transactions");
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne(x => x.Category).WithMany(c => c.Transactions)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Wallet).WithMany(w => w.Transactions)
                .HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany(u => u.Transactions)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.TransactionDate });
        });

        builder.Entity<Budget>(e =>
        {
            e.ToTable("Budgets");
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasOne(x => x.Category).WithMany(c => c.Budgets)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany(u => u.Budgets)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UserId, x.Year, x.Month });
        });

        builder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");
            e.Property(x => x.Token).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Soft delete: global query filter cho mọi entity kế thừa BaseEntity
        builder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Category>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Wallet>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Transaction>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Budget>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<RefreshToken>().HasQueryFilter(x => !x.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = now;
                    break;
                case EntityState.Deleted:
                    // Soft delete: chuyển Delete thành Update + IsDeleted = true
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedDate = now;
                    break;
            }
        }
        return base.SaveChangesAsync(ct);
    }
}
