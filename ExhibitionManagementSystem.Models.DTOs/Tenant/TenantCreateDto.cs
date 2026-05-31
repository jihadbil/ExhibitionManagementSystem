using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Tenant;

public class TenantCreateDto
{
    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Subdomain { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Plan { get; set; } = string.Empty;

    [StringLength(3)]
    public string BaseCurrency { get; set; } = string.Empty;

    public DateTime? TrialEndsAt { get; set; }
}
