using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Financial
{
    public class InvoiceItemCreateDto
    {
        [Required]
        [StringLength(150)]
        public string ItemName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }
}
