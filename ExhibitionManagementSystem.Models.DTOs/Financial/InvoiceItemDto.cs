using System;

namespace ExhibitionManagementSystem.Models.DTOs.Financial
{
    public class InvoiceItemDto
    {
        public int InvoiceItemID { get; set; }
        public int InvoiceID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
