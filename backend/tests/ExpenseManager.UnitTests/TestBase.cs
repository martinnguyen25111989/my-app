using AutoMapper;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Mappings;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ExpenseManager.UnitTests;

public abstract class TestBase : IDisposable
{
    protected readonly AppDbContext Context;
    protected readonly IUnitOfWork Uow;
    protected readonly IMapper Mapper;
    protected readonly Guid UserId = Guid.NewGuid();
    protected readonly Mock<ICurrentUserService> CurrentUser;

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"test_{Guid.NewGuid()}")
            .Options;
        Context = new AppDbContext(options);
        Uow = new UnitOfWork(Context);
        Mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        CurrentUser = new Mock<ICurrentUserService>();
        CurrentUser.Setup(x => x.UserId).Returns(UserId);
    }

    protected async Task<User> SeedUserAsync()
    {
        var user = new User { Id = UserId, Email = "test@example.com", FullName = "Test User", PasswordHash = "hash" };
        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        return user;
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
