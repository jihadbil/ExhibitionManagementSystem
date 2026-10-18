using AutoMapper;
using ExhibitionManagementSystem.Models.DTOs.Hall;
using ExhibitionManagementSystem.Models.DTOs.Mapping.Profiles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExhibitionManagementSystem.Api.Tests.Mapping;

public class HallMappingProfileTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<HallMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void Create_MapsRequestedActiveState()
    {
        var hall = Mapper.Map<ExhibitionManagementSystem.Models.Hall>(new HallCreateDto
        {
            HallName = "Maintenance Hall",
            IsActive = false
        });

        Assert.False(hall.IsActive);
    }

    [Fact]
    public void Update_WhenFloorPlanIsOmitted_PreservesExistingPlan()
    {
        var hall = new ExhibitionManagementSystem.Models.Hall
        {
            HallName = "Original",
            FloorPlanJSON = "{\"existing\":true}"
        };

        Mapper.Map(new HallUpdateDto
        {
            HallName = "Updated",
            FloorPlanJSON = null,
            IsActive = true
        }, hall);

        Assert.Equal("Updated", hall.HallName);
        Assert.Equal("{\"existing\":true}", hall.FloorPlanJSON);
    }

    [Fact]
    public void Update_WhenFloorPlanIsProvided_MapsTheNewPlan()
    {
        var hall = new ExhibitionManagementSystem.Models.Hall
        {
            HallName = "Original",
            FloorPlanJSON = "{\"existing\":true}"
        };

        Mapper.Map(new HallUpdateDto
        {
            HallName = "Updated",
            FloorPlanJSON = "{\"replacement\":true}",
            IsActive = true
        }, hall);

        Assert.Equal("{\"replacement\":true}", hall.FloorPlanJSON);
    }
}
