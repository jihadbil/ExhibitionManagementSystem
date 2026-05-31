using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibition;

public class ExhibitionCreateDto
{
    public int TenantID { get; set; }
    public int VenueID { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string Type { get; set; } = string.Empty;

    [StringLength(50)]
    public string Edition { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? ExpectedVisitors { get; set; }
    public decimal? EntryFee { get; set; }

    [StringLength(3)]
    public string EntryCurrency { get; set; } = string.Empty;
}
