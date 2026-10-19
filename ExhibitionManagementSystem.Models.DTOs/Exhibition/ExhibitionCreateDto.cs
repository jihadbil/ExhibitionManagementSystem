using ExhibitionManagementSystem.Models.DTOs.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibition;

public class ExhibitionCreateDto
{
    public int TenantID { get; set; }
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int VenueID { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string Type { get; set; } = string.Empty;

    [StringLength(50)]
    public string Edition { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    [DateAfter(nameof(StartDate))]
    public DateTime EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
    public int? ExpectedVisitors { get; set; }
    [DecimalValue("0", "9999999999999999.99")]
    public decimal? EntryFee { get; set; }

    [StringLength(3)]
    public string EntryCurrency { get; set; } = string.Empty;
}
