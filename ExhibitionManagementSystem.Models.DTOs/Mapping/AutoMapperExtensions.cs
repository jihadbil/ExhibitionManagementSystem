using Microsoft.Extensions.DependencyInjection;

namespace ExhibitionManagementSystem.Models.DTOs.Mapping;

public static class AutoMapperExtensions
{
    public static IServiceCollection AddDtoMapping(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(MappingProfile).Assembly);
        return services;
    }
}
