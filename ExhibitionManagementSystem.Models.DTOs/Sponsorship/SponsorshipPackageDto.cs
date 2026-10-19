using ExhibitionManagementSystem.Models.DTOs.Validation;
using System;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models.DTOs.Sponsorship;

public class SponsorshipPackageDto
{
    public int PackageID { get; set; }
    public int TenantID { get; set; }
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public SponsorshipLevel Level { get; set; }
    public string LevelName => Level.ToString();
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "LYD";
    public int MaxSponsorsCount { get; set; }
    public int AllocatedSponsorsCount { get; set; }
    public string? EntitlementsDescription { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoldOut => AllocatedSponsorsCount >= MaxSponsorsCount;
    public DateTime CreatedAt { get; set; }
}

public class SponsorshipPackageCreateDto
{
    public int ExhibitionID { get; set; }
    public string PackageName { get; set; } = string.Empty;
    [DefinedEnum(typeof(SponsorshipLevel))]
    public SponsorshipLevel Level { get; set; } = SponsorshipLevel.Gold;
    [DecimalValue("0", "9999999999999999.99")]
    public decimal Price { get; set; }
    public string CurrencyCode { get; set; } = "LYD";
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int MaxSponsorsCount { get; set; } = 1;
    public string? EntitlementsDescription { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SponsorshipPackageUpdateDto : SponsorshipPackageCreateDto
{
}
