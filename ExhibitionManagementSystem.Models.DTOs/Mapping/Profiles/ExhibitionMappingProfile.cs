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
            .ForMember(dest => dest.VenueID, opt => opt.Condition(src => src.VenueID.HasValue && src.VenueID.Value > 0))
            .ForMember(dest => dest.Edition, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Edition)))
            .ForMember(dest => dest.ExpectedVisitors, opt => opt.Condition(src => src.ExpectedVisitors.HasValue))
            .ForMember(dest => dest.EntryFee, opt => opt.Condition(src => src.EntryFee.HasValue))
            .ForMember(dest => dest.EntryCurrency, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.EntryCurrency)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src =>
                string.IsNullOrWhiteSpace(src.Status) ? ExhibitionStatus.Planning :
                src.Status.Equals("Open", StringComparison.OrdinalIgnoreCase) || src.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) ? ExhibitionStatus.Open :
                src.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase) || src.Status.Equals("Ended", StringComparison.OrdinalIgnoreCase) ? ExhibitionStatus.Closed :
                src.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || src.Status.Equals("Canceled", StringComparison.OrdinalIgnoreCase) ? ExhibitionStatus.Cancelled :
                ExhibitionStatus.Planning))
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
            .ForMember(dest => dest.EventType, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.EventType) ? (EventType?)Enum.Parse<EventType>(src.EventType, true) : null))
            .ForMember(dest => dest.Exhibition, opt => opt.Ignore())
            .ForMember(dest => dest.Hall, opt => opt.Ignore());
    }
}
