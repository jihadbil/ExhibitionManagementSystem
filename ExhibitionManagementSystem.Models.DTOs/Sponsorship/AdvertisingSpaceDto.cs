using System;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models.DTOs.Sponsorship;

public class AdvertisingSpaceDto
{
    public int SpaceID { get; set; }
    public int TenantID { get; set; }
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public int? VenueID { get; set; }
    public string? VenueName { get; set; }
    public int? HallID { get; set; }
    public string? HallName { get; set; }
    public string SpaceCode { get; set; } = string.Empty;
    public string SpaceName { get; set; } = string.Empty;
    public AdvertisingSpaceType SpaceType { get; set; }
    public string SpaceTypeName => SpaceType.ToString();
    public string? DimensionsDescription { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "LYD";
    public bool IsAvailable { get; set; }
    public string? LocationNotes { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CurrentSponsorName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdvertisingSpaceCreateDto
{
    public int ExhibitionID { get; set; }
    public int? VenueID { get; set; }
    public int? HallID { get; set; }
    public string SpaceCode { get; set; } = string.Empty;
    public string SpaceName { get; set; } = string.Empty;
    public AdvertisingSpaceType SpaceType { get; set; } = AdvertisingSpaceType.DigitalScreen;
    public string? DimensionsDescription { get; set; }
    public decimal BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "LYD";
    public bool IsAvailable { get; set; } = true;
    public string? LocationNotes { get; set; }
    public string? PhotoUrl { get; set; }
}

public class AdvertisingSpaceUpdateDto : AdvertisingSpaceCreateDto
{
}
