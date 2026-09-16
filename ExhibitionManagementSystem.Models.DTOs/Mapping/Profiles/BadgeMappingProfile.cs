using AutoMapper;
using ExhibitionManagementSystem.Models;
using ExhibitionManagementSystem.Models.DTOs.Badge;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;

public class BadgeMappingProfile : Profile
{
    public BadgeMappingProfile()
    {
        CreateMap<BadgeTemplate, BadgeTemplateDto>()
            .ForMember(dest => dest.ExhibitionName, opt => opt.MapFrom(src => src.Exhibition != null ? src.Exhibition.Name : null));

        CreateMap<BadgeTemplateCreateDto, BadgeTemplate>();
        CreateMap<BadgeTemplateUpdateDto, BadgeTemplate>();
    }
}
