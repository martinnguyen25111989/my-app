using AutoMapper;
using AutoMapper.QueryableExtensions;
using ExpenseManager.Application.Common.Exceptions;
using ExpenseManager.Application.Common.Interfaces;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Entities;
using ExpenseManager.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseManager.Application.Features.Categories;

// ===== Query (search + pagination) =====
public record GetCategoriesQuery(string? Search, TransactionType? Type, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<CategoryDto>>;

public class GetCategoriesQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<GetCategoriesQuery, PagedResult<CategoryDto>>
{
    public async Task<PagedResult<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var query = uow.Categories.Query().Where(c => c.UserId == currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search));
        }
        if (request.Type.HasValue)
            query = query.Where(c => c.Type == request.Type.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Type).ThenBy(c => c.Name)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .ProjectTo<CategoryDto>(mapper.ConfigurationProvider)
            .ToListAsync(ct);

        return new PagedResult<CategoryDto>
        {
            Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }
}

// ===== Create =====
public record CreateCategoryCommand(string Name, TransactionType Type, string Icon, string Color)
    : IRequest<CategoryDto>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9a-fA-F]{6}$");
    }
}

public class CreateCategoryCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        var exists = await uow.Categories.Query()
            .AnyAsync(c => c.UserId == currentUser.UserId && c.Name == name && c.Type == request.Type, ct);
        if (exists)
            throw new ConflictException("Danh mục đã tồn tại.");

        var category = new Category
        {
            Name = name, Type = request.Type, Icon = request.Icon, Color = request.Color,
            UserId = currentUser.UserId
        };
        await uow.Categories.AddAsync(category, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CategoryDto>(category);
    }
}

// ===== Update =====
public record UpdateCategoryCommand(Guid Id, string Name, TransactionType Type, string Icon, string Color)
    : IRequest<CategoryDto>;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Color).NotEmpty().Matches("^#[0-9a-fA-F]{6}$");
    }
}

public class UpdateCategoryCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IMapper mapper)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await uow.Categories.Query()
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy danh mục.");

        category.Name = request.Name.Trim();
        category.Type = request.Type;
        category.Icon = request.Icon;
        category.Color = request.Color;
        uow.Categories.Update(category);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<CategoryDto>(category);
    }
}

// ===== Delete (soft delete) =====
public record DeleteCategoryCommand(Guid Id) : IRequest;

public class DeleteCategoryCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await uow.Categories.Query()
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == currentUser.UserId, ct)
            ?? throw new NotFoundException("Không tìm thấy danh mục.");

        var hasTransactions = await uow.Transactions.Query()
            .AnyAsync(t => t.CategoryId == category.Id, ct);
        if (hasTransactions)
            throw new ConflictException("Danh mục đang có giao dịch, không thể xóa.");

        uow.Categories.Remove(category);
        await uow.SaveChangesAsync(ct);
    }
}
