using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Hall;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class HallMappingProfile : Profile
{
    public HallMappingProfile()
    {
        CreateMap<Models.Hall, HallDto>()
            .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Venue != null ? src.Venue.Name : string.Empty))
            .ForMember(dest => dest.BoothsCount, opt => opt.MapFrom(src => src.Booths != null ? src.Booths.Count : 0));

        CreateMap<Models.Hall, HallSummaryDto>()
            .ForMember(dest => dest.BoothsCount, opt => opt.MapFrom(src => src.Booths != null ? src.Booths.Count : 0));

        CreateMap<HallCreateDto, Models.Hall>()
            .ForMember(dest => dest.HallID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Venue, opt => opt.Ignore())
            .ForMember(dest => dest.Booths, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<HallUpdateDto, Models.Hall>()
            .ForMember(dest => dest.HallID, opt => opt.Ignore())
            .ForMember(dest => dest.VenueID, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Venue, opt => opt.Ignore())
            .ForMember(dest => dest.Booths, opt => opt.Ignore());
    }
}
