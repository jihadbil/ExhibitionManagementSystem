using System;
using ExhibitionManagementSystem.Models.DTOs.Common;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibitor;

public class ExhibitorDto : AuditDto
{
    public int ExhibitorID { get; set; }
    public int TenantID { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string ExhibitorCategory { get; set; } = string.Empty;
    public string LogoURL { get; set; } = string.Empty;
    public string CompanyProfile { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? UserId { get; set; }
}
