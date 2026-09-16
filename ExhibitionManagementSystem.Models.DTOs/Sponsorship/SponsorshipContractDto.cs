using System;
using System.Collections.Generic;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models.DTOs.Sponsorship;

public class SponsorshipContractDto
{
    public int ContractID { get; set; }
    public int TenantID { get; set; }
    public int ExhibitionID { get; set; }
    public string ExhibitionName { get; set; } = string.Empty;
    public int SponsorID { get; set; }
    public string SponsorCompanyName { get; set; } = string.Empty;
    public string? SponsorLogoUrl { get; set; }
    public int? PackageID { get; set; }
    public string? PackageName { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "LYD";
    public SponsorshipStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime SignedDate { get; set; }
    public DateTime? PaymentDueDate { get; set; }
    public string? SpecialTerms { get; set; }
    public string? Notes { get; set; }
    public List<SponsorAdAssignmentDto> AdAssignments { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class SponsorshipContractCreateDto
{
    public int ExhibitionID { get; set; }
    public int SponsorID { get; set; }
    public int? PackageID { get; set; }
    public string? ContractNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = "LYD";
    public SponsorshipStatus Status { get; set; } = SponsorshipStatus.Confirmed;
    public DateTime? PaymentDueDate { get; set; }
    public string? SpecialTerms { get; set; }
    public string? Notes { get; set; }
    public List<int> SpaceIDsToAssign { get; set; } = [];
}

public class SponsorAdAssignmentDto
{
    public int AssignmentID { get; set; }
    public int ContractID { get; set; }
    public int SpaceID { get; set; }
    public string SpaceCode { get; set; } = string.Empty;
    public string SpaceName { get; set; } = string.Empty;
    public AdvertisingSpaceType SpaceType { get; set; }
    public DateTime AssignedDate { get; set; }
    public string AssetStatus { get; set; } = "Pending";
    public string? ArtworkFileUrl { get; set; }
    public string? Notes { get; set; }
}

public class SponsorAdAssignmentCreateDto
{
    public int ContractID { get; set; }
    public int SpaceID { get; set; }
    public string? ArtworkFileUrl { get; set; }
    public string? Notes { get; set; }
}
