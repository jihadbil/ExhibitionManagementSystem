using System;

namespace ExhibitionManagementSystem.Models.DTOs.Sponsorship;

public class SponsorDto
{
    public int SponsorID { get; set; }
    public int TenantID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? CompanyProfile { get; set; }
    public bool IsActive { get; set; }
    public int TotalContractsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SponsorCreateDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? CompanyProfile { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SponsorUpdateDto : SponsorCreateDto
{
}
