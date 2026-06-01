using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExhibitionManagementSystem.Models.DTOs.Financial;

public class ExpenseCreateDto
{
    public int ExhibitionID { get; set; }
    [Required, StringLength(200)] public string Description { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Category { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}
