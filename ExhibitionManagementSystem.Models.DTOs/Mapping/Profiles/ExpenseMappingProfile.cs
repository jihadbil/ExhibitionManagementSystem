using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Financial;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class ExpenseMappingProfile : Profile
{
    public ExpenseMappingProfile()
    {
        CreateMap<Expense, ExpenseDto>()
            .ForMember(dest => dest.ExhibitionName, 
                       opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : string.Empty));
        CreateMap<ExpenseCreateDto, Expense>();
    }
}
