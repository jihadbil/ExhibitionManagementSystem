using System;
using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Visitor;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class VisitorMappingProfile : Profile
{
    public VisitorMappingProfile()
    {
        CreateMap<Models.Visitor, VisitorDto>()
            .ForMember(dest => dest.TicketsCount, opt => opt.MapFrom(src => src.Tickets != null ? src.Tickets.Count : 0));

        CreateMap<VisitorCreateDto, Models.Visitor>()
            .ForMember(dest => dest.VisitorID, opt => opt.Ignore())
            .ForMember(dest => dest.RegisteredAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Tickets, opt => opt.Ignore());

        CreateMap<Ticket, TicketDto>()
            .ForMember(dest => dest.VisitorName, opt => opt.MapFrom(src => src.Visitor != null ? src.Visitor.FullName : string.Empty))
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : string.Empty))
            .ForMember(dest => dest.CurrencySymbol, opt => opt.MapFrom(src => src.Currency != null ? src.Currency.Symbol : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ScansCount, opt => opt.MapFrom(src => src.TicketScans != null ? src.TicketScans.Count : 0));

        CreateMap<TicketCreateDto, Ticket>()
            .ForMember(dest => dest.TicketID, opt => opt.Ignore())
            .ForMember(dest => dest.QRCode, opt => opt.MapFrom(src => Guid.NewGuid().ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => TicketStatus.Active))
            .ForMember(dest => dest.IssuedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.Visitor, opt => opt.Ignore())
            .ForMember(dest => dest.Exhibition, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.TicketScans, opt => opt.Ignore());

        CreateMap<TicketScan, TicketScanDto>()
            .ForMember(dest => dest.ScanLocation, opt => opt.MapFrom(src => src.GateName))
            .ForMember(dest => dest.ScanTime, opt => opt.MapFrom(src => src.ScanDateTime))
            .ForMember(dest => dest.ScanDirection, opt => opt.MapFrom(src => src.Direction.ToString()))
            .ForMember(dest => dest.QRCode, opt => opt.MapFrom(src => src.Ticket != null ? src.Ticket.QRCode : string.Empty));

        CreateMap<VisitorRating, VisitorRatingDto>()
            .ForMember(dest => dest.VisitorName, opt => opt.MapFrom(src => src.Visitor != null ? src.Visitor.FullName : string.Empty))
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : string.Empty))
            .ForMember(dest => dest.Rating, opt => opt.MapFrom(src => src.Score));
    }
}
