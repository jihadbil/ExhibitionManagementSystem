using System;
using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Reservation;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class ReservationMappingProfile : Profile
{
    public ReservationMappingProfile()
    {
        CreateMap<BoothReservation, BoothReservationDto>()
            .ForMember(dest => dest.ExhibitorName, opt => opt.MapFrom(src => src.Exhibitor != null ? src.Exhibitor.CompanyName : string.Empty))
            .ForMember(dest => dest.BoothNumber, opt => opt.MapFrom(src => src.Booth != null ? src.Booth.BoothNumber : string.Empty))
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : string.Empty))
            .ForMember(dest => dest.CurrencySymbol, opt => opt.MapFrom(src => src.Currency != null ? src.Currency.Symbol : string.Empty))
            .ForMember(dest => dest.BoothTypeSelected, opt => opt.MapFrom(src => src.BoothTypeSelected.ToString()))
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => src.ExhibitorCategory.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Services, opt => opt.MapFrom(src => src.ReservationServices));

        CreateMap<BoothReservation, BoothReservationSummaryDto>()
            .ForMember(dest => dest.ExhibitorName, opt => opt.MapFrom(src => src.Exhibitor != null ? src.Exhibitor.CompanyName : string.Empty))
            .ForMember(dest => dest.BoothNumber, opt => opt.MapFrom(src => src.Booth != null ? src.Booth.BoothNumber : string.Empty))
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<BoothReservationCreateDto, BoothReservation>()
            .ForMember(dest => dest.ReservationID, opt => opt.Ignore())
            .ForMember(dest => dest.BoothTypeSelected, opt => opt.MapFrom(src => Enum.Parse<BoothType>(src.BoothTypeSelected, true)))
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.MapFrom(src => Enum.Parse<ExhibitorCategory>(src.ExhibitorCategory, true)))
            .ForMember(dest => dest.AllocatedAreaSqM, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.BoothAmount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.ServicesAmount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.ExchangeRateUsed, opt => opt.MapFrom(src => 1))
            .ForMember(dest => dest.AmountInBaseCurrency, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ReservationStatus.Pending))
            .ForMember(dest => dest.ReservationDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Exhibitor, opt => opt.Ignore())
            .ForMember(dest => dest.Booth, opt => opt.Ignore())
            .ForMember(dest => dest.Exhibition, opt => opt.Ignore())
            .ForMember(dest => dest.BoothMerge, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Invoice, opt => opt.Ignore())
            .ForMember(dest => dest.ReservationServices, opt => opt.Ignore());

        CreateMap<BoothReservationUpdateDto, BoothReservation>()
            .ForMember(dest => dest.ReservationID, opt => opt.Ignore())
            .ForMember(dest => dest.ExhibitorID, opt => opt.Ignore())
            .ForMember(dest => dest.ExhibitionID, opt => opt.Ignore())
            .ForMember(dest => dest.BoothTypeSelected, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedAreaSqM, opt => opt.Ignore())
            .ForMember(dest => dest.ExhibitorCategory, opt => opt.Ignore())
            .ForMember(dest => dest.CurrencyCode, opt => opt.Ignore())
            .ForMember(dest => dest.ExchangeRateUsed, opt => opt.Ignore())
            .ForMember(dest => dest.AmountInBaseCurrency, opt => opt.Ignore())
            .ForMember(dest => dest.ReservationDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Exhibitor, opt => opt.Ignore())
            .ForMember(dest => dest.Booth, opt => opt.Ignore())
            .ForMember(dest => dest.Exhibition, opt => opt.Ignore())
            .ForMember(dest => dest.BoothMerge, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Invoice, opt => opt.Ignore())
            .ForMember(dest => dest.ReservationServices, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<ReservationStatus>(src.Status, true)));

        CreateMap<ReservationService, ReservationServiceDto>()
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service != null ? src.Service.ServiceName : string.Empty));

        CreateMap<ReservationServiceCreateDto, ReservationService>()
            .ForMember(dest => dest.ReservationServiceID, opt => opt.Ignore())
            .ForMember(dest => dest.ReservationID, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Quantity * src.UnitPrice))
            .ForMember(dest => dest.Reservation, opt => opt.Ignore())
            .ForMember(dest => dest.Service, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore());
    }
}
