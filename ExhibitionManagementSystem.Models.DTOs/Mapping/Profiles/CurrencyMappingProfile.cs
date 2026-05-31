using System;
using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Currency;
using ExhibitionManagementSystem.Models.DTOs.Financial;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class CurrencyMappingProfile : Profile
{
    public CurrencyMappingProfile()
    {
        CreateMap<Models.Currency, CurrencyDto>();

        CreateMap<ExchangeRate, ExchangeRateDto>()
            .ForMember(dest => dest.ExchangeRateID, opt => opt.MapFrom(src => src.RateID))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.RateDate))
            .ForMember(dest => dest.ValidTo, opt => opt.MapFrom(src => (DateTime?)null))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
    }
}
