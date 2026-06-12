using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Domain.Common;
using ExpenseManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Infrastructure.Persistence;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly DbSet<T> Set = context.Set<T>();

    public IQueryable<T> Query() => Set.AsQueryable();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    private IRepository<User>? _users;
    private IRepository<Category>? _categories;
    private IRepository<Transaction>? _transactions;
    private IRepository<Wallet>? _wallets;
    private IRepository<Budget>? _budgets;
    private IRepository<RefreshToken>? _refreshTokens;

    public IRepository<User> Users => _users ??= new Repository<User>(context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(context);
    public IRepository<Transaction> Transactions => _transactions ??= new Repository<Transaction>(context);
    public IRepository<Wallet> Wallets => _wallets ??= new Repository<Wallet>(context);
    public IRepository<Budget> Budgets => _budgets ??= new Repository<Budget>(context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(context);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
