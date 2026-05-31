using System;
using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Exhibition;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class ExhibitionMappingProfile : Profile
{
    public ExhibitionMappingProfile()
    {
        CreateMap<Models.Exhibition, ExhibitionDto>()
            .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Venue != null ? src.Venue.Name : string.Empty))
            .ForMember(dest => dest.CurrencySymbol, opt => opt.MapFrom(src => src.Currency != null ? src.Currency.Symbol : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Models.Exhibition, ExhibitionSummaryDto>()
            .ForMember(dest => dest.VenueName, opt => opt.MapFrom(src => src.Venue != null ? src.Venue.Name : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<ExhibitionCreateDto, Models.Exhibition>()
            .ForMember(dest => dest.ExhibitionID, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ExhibitionStatus.Planning))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Venue, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.ExhibitionSchedules, opt => opt.Ignore())
            .ForMember(dest => dest.BoothReservations, opt => opt.Ignore());

        CreateMap<ExhibitionUpdateDto, Models.Exhibition>()
            .ForMember(dest => dest.ExhibitionID, opt => opt.Ignore())
            .ForMember(dest => dest.TenantID, opt => opt.Ignore())
            .ForMember(dest => dest.VenueID, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<ExhibitionStatus>(src.Status, true)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Venue, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.ExhibitionSchedules, opt => opt.Ignore())
            .ForMember(dest => dest.BoothReservations, opt => opt.Ignore());

        CreateMap<ExhibitionSchedule, ExhibitionScheduleDto>()
            .ForMember(dest => dest.HallName, opt => opt.MapFrom(src => src.Hall != null ? src.Hall.HallName : string.Empty))
            .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => src.EventType.HasValue ? src.EventType.Value.ToString() : null));

        CreateMap<ExhibitionScheduleCreateDto, ExhibitionSchedule>()
            .ForMember(dest => dest.ScheduleID, opt => opt.Ignore())
            .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.EventType) ? (EventType?)Enum.Parse<EventType>(src.EventType, true) : null))
            .ForMember(dest => dest.Exhibition, opt => opt.Ignore())
            .ForMember(dest => dest.Hall, opt => opt.Ignore());
    }
}
