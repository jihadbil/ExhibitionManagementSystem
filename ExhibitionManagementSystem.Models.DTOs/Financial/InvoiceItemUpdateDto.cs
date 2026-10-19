using ExhibitionManagementSystem.Models.DTOs.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace ExhibitionManagementSystem.Models.DTOs.Financial
{
    public class InvoiceItemUpdateDto
    {
        public int InvoiceItemID { get; set; }

        [Required]
        [StringLength(150)]
        public string ItemName { get; set; } = string.Empty;

        [DecimalValue("0.01", "99999999.99")]
    public decimal Quantity { get; set; }

        [DecimalValue("0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }
    }
}
