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

        [Range(typeof(decimal), "0.01", "9999999999999999.99")]
        public decimal Quantity { get; set; }

        [Range(typeof(decimal), "0", "99999999999999999999.99999999")]
        public decimal UnitPrice { get; set; }
    }
}
