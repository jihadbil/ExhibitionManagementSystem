using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Visitor;

public class TicketCreateDto
{
    [Range(1, int.MaxValue)]
    public int VisitorID { get; set; }

    [Range(1, int.MaxValue)]
    public int ExhibitionID { get; set; }

    [Required]
    [StringLength(50)]
    public string TicketType { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal Price { get; set; }

    [Required]
    [StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public DateTime? ValidDate { get; set; }
}
