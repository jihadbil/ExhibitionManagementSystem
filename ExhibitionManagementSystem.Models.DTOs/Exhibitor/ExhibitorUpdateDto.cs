using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Exhibitor;

public class ExhibitorUpdateDto
{
    [Required]
    [StringLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContactPerson { get; set; } = string.Empty;

    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [StringLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string Sector { get; set; } = string.Empty;

    [StringLength(100)]
    public string Nationality { get; set; } = string.Empty;

    [Required]
    public string ExhibitorCategory { get; set; } = string.Empty;

    [StringLength(500)]
    public string LogoURL { get; set; } = string.Empty;

    public string CompanyProfile { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
