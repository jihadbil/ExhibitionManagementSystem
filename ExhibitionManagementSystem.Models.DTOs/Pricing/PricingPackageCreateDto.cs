using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Pricing;

public class PricingPackageCreateDto
{
    public int TenantID { get; set; }

    [Required]
    [StringLength(200)]
    public string PackageName { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    [Required]
    public List<int> ServiceIDs { get; set; } = [];
}
