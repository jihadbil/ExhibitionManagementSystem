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
        CreateMap<ExpenseCreateDto, Expense>()
            .ForMember(dest => dest.ExpenseID, opt => opt.Ignore())
            .ForMember(dest => dest.TenantID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Exhibition, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore());
    }
}
