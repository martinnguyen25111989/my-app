using AutoMapper;
using ExpenseManager.Application.Common.Models;
using ExpenseManager.Domain.Entities;

namespace ExpenseManager.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<Category, CategoryDto>();
        CreateMap<Wallet, WalletDto>()
            .ForMember(d => d.CurrentBalance, o => o.Ignore());
        CreateMap<Transaction, TransactionDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.CategoryIcon, o => o.MapFrom(s => s.Category.Icon))
            .ForMember(d => d.CategoryColor, o => o.MapFrom(s => s.Category.Color))
            .ForMember(d => d.WalletName, o => o.MapFrom(s => s.Wallet.Name));
        CreateMap<Budget, BudgetDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.CategoryColor, o => o.MapFrom(s => s.Category.Color))
            .ForMember(d => d.Spent, o => o.Ignore());
    }
}
