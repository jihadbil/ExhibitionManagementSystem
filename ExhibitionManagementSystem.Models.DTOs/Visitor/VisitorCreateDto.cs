using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Visitor;

public class VisitorCreateDto
{
    public int TenantID { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string Nationality { get; set; } = string.Empty;

    [StringLength(50)]
    public string VisitorType { get; set; } = string.Empty;
}
