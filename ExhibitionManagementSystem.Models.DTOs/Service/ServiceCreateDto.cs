using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Service;

public class ServiceCreateDto
{
    public int TenantID { get; set; }

    [Required]
    [StringLength(200)]
    public string ServiceName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(50)]
    public string Unit { get; set; } = string.Empty;

    public decimal? DefaultPrice { get; set; }
    public bool IsMandatory { get; set; }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}
