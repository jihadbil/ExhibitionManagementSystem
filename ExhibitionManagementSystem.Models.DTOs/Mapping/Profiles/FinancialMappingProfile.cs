using System;
using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.DTOs.Financial;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class FinancialMappingProfile : Profile
{
    public FinancialMappingProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(dest => dest.ExhibitorName, opt => opt.MapFrom(src => src.Reservation != null && src.Reservation.Exhibitor != null ? src.Reservation.Exhibitor.CompanyName : string.Empty))
            .ForMember(dest => dest.CurrencySymbol, opt => opt.MapFrom(src => src.Currency != null ? src.Currency.Symbol : string.Empty))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments));

        CreateMap<InvoiceCreateDto, Invoice>()
            .ForMember(dest => dest.InvoiceID, opt => opt.Ignore())
            .ForMember(dest => dest.InvoiceDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.TaxAmount, opt => opt.MapFrom(src => src.SubTotal * (src.TaxRate / 100)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => InvoiceStatus.Draft))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Tenant, opt => opt.Ignore())
            .ForMember(dest => dest.Reservation, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore());

        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.InvoiceNumber, opt => opt.MapFrom(src => src.Invoice != null ? src.Invoice.InvoiceNumber : string.Empty))
            .ForMember(dest => dest.CurrencySymbol, opt => opt.MapFrom(src => src.Currency != null ? src.Currency.Symbol : string.Empty))
            .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.Method.ToString()))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ReceivedByName, opt => opt.MapFrom(src => src.ReceivedByUser != null ? src.ReceivedByUser.FullName : string.Empty));

        CreateMap<PaymentCreateDto, Payment>()
            .ForMember(dest => dest.PaymentID, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => PaymentStatus.Completed))
            .ForMember(dest => dest.Method, opt => opt.MapFrom(src => Enum.Parse<PaymentMethod>(src.Method, true)))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Invoice, opt => opt.Ignore())
            .ForMember(dest => dest.Currency, opt => opt.Ignore())
            .ForMember(dest => dest.ReceivedByUser, opt => opt.Ignore());

        CreateMap<FinancialReport, FinancialReportDto>()
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : string.Empty));
    }
}
