using AutoMapper;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Central mapping configuration registry.
        // AutoMapper automatically registers all Profile subclasses found in the Assembly,
        // but we can also use this central profile or individual profiles.
    }
}
