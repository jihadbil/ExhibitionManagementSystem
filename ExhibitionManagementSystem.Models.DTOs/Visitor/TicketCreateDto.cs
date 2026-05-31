using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Visitor;

public class TicketCreateDto
{
    public int VisitorID { get; set; }
    public int ExhibitionID { get; set; }

    [Required]
    [StringLength(50)]
    public string TicketType { get; set; } = string.Empty;

    public decimal Price { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public DateTime? ValidDate { get; set; }
}
