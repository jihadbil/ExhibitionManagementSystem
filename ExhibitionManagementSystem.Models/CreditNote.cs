using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// مستند مالي غير قابل للتعديل يعكس تخفيضًا أو استردادًا مرتبطًا بفاتورة صادرة.
/// </summary>
public sealed class CreditNote : ITenantEntity, IAuditableEntity
{
    [Key]
    public int CreditNoteID { get; set; }
    public int TenantID { get; set; }
    public int InvoiceID { get; set; }

    [Required, StringLength(50)]
    public string CreditNoteNumber { get; set; } = string.Empty;

    public CreditNoteType Type { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, StringLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required, StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(TenantID))]
    public Tenant Tenant { get; set; } = null!;

    [ForeignKey(nameof(InvoiceID))]
    public Invoice Invoice { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public ApplicationUser CreatedByUser { get; set; } = null!;
}
