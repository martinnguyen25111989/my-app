using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Features.Categories;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseManager.UnitTests;

public class CategoryTests : TestBase
{
    [Fact]
    public async Task CreateCategory_ShouldPersist_WhenValid()
    {
        await SeedUserAsync();
        var handler = new CreateCategoryCommandHandler(Uow, CurrentUser.Object, Mapper);

        var result = await handler.Handle(
            new CreateCategoryCommand("Ăn uống", TransactionType.Expense, "restaurant", "#e53935"),
            CancellationToken.None);

        result.Name.Should().Be("Ăn uống");
        (await Context.Categories.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateCategory_ShouldThrowConflict_WhenDuplicateName()
    {
        await SeedUserAsync();
        var handler = new CreateCategoryCommandHandler(Uow, CurrentUser.Object, Mapper);
        var command = new CreateCategoryCommand("Ăn uống", TransactionType.Expense, "restaurant", "#e53935");

        await handler.Handle(command, CancellationToken.None);
        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task DeleteCategory_ShouldSoftDelete()
    {
        await SeedUserAsync();
        var category = new Category { Name = "Test", Type = TransactionType.Expense, UserId = UserId };
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var handler = new DeleteCategoryCommandHandler(Uow, CurrentUser.Object);
        await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        // Query filter ẩn bản ghi đã xóa mềm
        (await Context.Categories.CountAsync()).Should().Be(0);
        var raw = await Context.Categories.IgnoreQueryFilters().FirstAsync(c => c.Id == category.Id);
        raw.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCategory_ShouldThrowNotFound_WhenNotOwned()
    {
        await SeedUserAsync();
        var otherUser = new User { Email = "other@example.com", FullName = "Other", PasswordHash = "h" };
        var category = new Category { Name = "Khác", Type = TransactionType.Expense, UserId = otherUser.Id };
        Context.Users.Add(otherUser);
        Context.Categories.Add(category);
        await Context.SaveChangesAsync();

        var handler = new DeleteCategoryCommandHandler(Uow, CurrentUser.Object);
        var act = () => handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCategories_ShouldFilterBySearchAndPaginate()
    {
        await SeedUserAsync();
        Context.Categories.AddRange(
            new Category { Name = "Ăn uống", Type = TransactionType.Expense, UserId = UserId },
            new Category { Name = "Di chuyển", Type = TransactionType.Expense, UserId = UserId },
            new Category { Name = "Lương", Type = TransactionType.Income, UserId = UserId });
        await Context.SaveChangesAsync();

        var handler = new GetCategoriesQueryHandler(Uow, CurrentUser.Object, Mapper);

        var all = await handler.Handle(new GetCategoriesQuery(null, null, 1, 2), CancellationToken.None);
        all.TotalCount.Should().Be(3);
        all.Items.Should().HaveCount(2);
        all.TotalPages.Should().Be(2);

        var searched = await handler.Handle(new GetCategoriesQuery("lương", null), CancellationToken.None);
        searched.Items.Should().ContainSingle(c => c.Name == "Lương");
    }
}
