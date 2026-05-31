using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Venue;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class VenueMappingProfile : Profile
{
    public VenueMappingProfile()
    {
        CreateMap<Models.Venue, VenueDto>()
            .ForMember(dest => dest.HallsCount, opt => opt.MapFrom(src => src.Halls != null ? src.Halls.Count : 0));

        CreateMap<Models.Venue, VenueSummaryDto>()
            .ForMember(dest => dest.HallsCount, opt => opt.MapFrom(src => src.Halls != null ? src.Halls.Count : 0));

        CreateMap<VenueCreateDto, Models.Venue>()
            .ForMember(dest => dest.VenueID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Halls, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<VenueUpdateDto, Models.Venue>()
            .ForMember(dest => dest.VenueID, opt => opt.Ignore())
            .ForMember(dest => dest.TenantID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Halls, opt => opt.Ignore());
    }
}
