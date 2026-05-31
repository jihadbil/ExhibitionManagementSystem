using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Payment : IAuditableEntity
{
    [Key] public int PaymentID { get; set; }
    public int InvoiceID { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; }
    public PaymentMethod Method { get; set; }
    [StringLength(100)] public string ReferenceNo { get; set; }
    public PaymentStatus Status { get; set; }
    [StringLength(500)] public string Notes { get; set; }
    [StringLength(450)] public string ReceivedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(InvoiceID))] public virtual Invoice Invoice { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }
    [ForeignKey(nameof(ReceivedByUserId))] public virtual ApplicationUser ReceivedByUser { get; set; }

}
