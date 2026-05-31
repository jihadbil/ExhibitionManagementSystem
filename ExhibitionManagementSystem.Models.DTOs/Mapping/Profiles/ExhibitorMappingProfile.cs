using System;
using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Exhibitor;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class ExhibitorMappingProfile : Profile
{
    public ExhibitorMappingProfile()
    {
        CreateMap<Models.Exhibitor, ExhibitorDto>()
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => src.ExhibitorCategory.ToString()));

        CreateMap<Models.Exhibitor, ExhibitorSummaryDto>()
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => src.ExhibitorCategory.ToString()));

        CreateMap<ExhibitorCreateDto, Models.Exhibitor>()
            .ForMember(dest => dest.ExhibitorID, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => Enum.Parse<ExhibitorCategory>(src.ExhibitorCategory, true)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.BoothReservations, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<ExhibitorUpdateDto, Models.Exhibitor>()
            .ForMember(dest => dest.ExhibitorID, opt => opt.Ignore())
            .ForMember(dest => dest.TenantID, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => Enum.Parse<ExhibitorCategory>(src.ExhibitorCategory, true)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.BoothReservations, opt => opt.Ignore());
    }
}
