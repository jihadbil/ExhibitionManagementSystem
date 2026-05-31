using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Admin;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class AdminMappingProfile : Profile
{
    public AdminMappingProfile()
    {
        CreateMap<TenantSubscription, TenantSubscriptionDto>()
            .ForMember(dest => dest.SubscriptionID, opt => opt.MapFrom(src => src.SubID))
            .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.CompanyName : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.MonthlyFee));

        CreateMap<AuditLog, AuditLogDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty));

        CreateMap<ApplicationUser, ApplicationUserDto>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.TenantName, opt => opt.MapFrom(src => src.Tenant != null ? src.Tenant.CompanyName : string.Empty))
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => new System.Collections.Generic.List<string>()));
    }
}
