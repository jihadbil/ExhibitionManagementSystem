using System;
using System.Linq;
using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Pricing;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class PricingMappingProfile : Profile
{
    public PricingMappingProfile()
    {
        CreateMap<BoothPriceRule, BoothPriceRuleDto>()
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : null))
            .ForMember(dest => dest.BoothType, opt => opt.MapFrom(src => src.BoothType.HasValue ? src.BoothType.Value.ToString() : null))
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => src.ExhibitorCategory.HasValue ? src.ExhibitorCategory.Value.ToString() : null));

        CreateMap<BoothPriceRuleCreateDto, BoothPriceRule>()
            .ForMember(dest => dest.RuleID, opt => opt.Ignore())
            .ForMember(dest => dest.BoothType, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.BoothType) ? (BoothType?)Enum.Parse<BoothType>(src.BoothType, true) : null))
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ExhibitorCategory) ? (ExhibitorCategory?)Enum.Parse<ExhibitorCategory>(src.ExhibitorCategory, true) : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Exhibition, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore());

        CreateMap<ServicePriceRule, ServicePriceRuleDto>()
            .ForMember(dest => dest.RuleID, opt => opt.MapFrom(src => src.PriceRuleID))
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service != null ? src.Service.ServiceName : string.Empty))
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : null))
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => src.ExhibitorCategory.HasValue ? src.ExhibitorCategory.Value.ToString() : null))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => string.Empty)); // Domain model doesn't have Notes, but DTO does.

        CreateMap<ServicePriceRuleCreateDto, ServicePriceRule>()
            .ForMember(dest => dest.PriceRuleID, opt => opt.Ignore())
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.ExhibitorCategory) ? (ExhibitorCategory?)Enum.Parse<ExhibitorCategory>(src.ExhibitorCategory, true) : null))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Service, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Exhibition, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore());

        CreateMap<PricingPackage, PricingPackageDto>()
            .ForMember(dest => dest.CurrencySymbol, opt => opt.MapFrom(src => src.Currency != null ? src.Currency.Symbol : string.Empty))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.PackageServices));

        CreateMap<PricingPackageCreateDto, PricingPackage>()
            .ForMember(dest => dest.PackageID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.PackageServices, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<PackageService, PackageServiceItemDto>()
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service != null ? src.Service.ServiceName : string.Empty));
    }
}
