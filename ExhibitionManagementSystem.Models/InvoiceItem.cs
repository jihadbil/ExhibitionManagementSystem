using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExhibitionManagementSystem.Models
{
    /// <summary>
    /// يمثل بندًا تفصيليًا داخل الفاتورة المالية.
    /// </summary>
    public class InvoiceItem
    {
        /// <summary>
        /// المعرف الفريد لبند الفاتورة.
        /// </summary>
        [Key]
        public int InvoiceItemID { get; set; }

        /// <summary>
        /// معرف الفاتورة التي ينتمي إليها هذا البند.
        /// </summary>
        public int InvoiceID { get; set; }

        /// <summary>
        /// اسم البند أو وصف الخدمة/الجناح المالي.
        /// </summary>
        [Required]
        [StringLength(150)]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// الكمية المحددة للبند.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// سعر وحدة البند.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// إجمالي سعر البند (الكمية × سعر الوحدة).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// الفاتورة المرتبطة بالبند.
        /// </summary>
        [ForeignKey(nameof(InvoiceID))]
        public virtual Invoice Invoice { get; set; } = null!;
    }
}
