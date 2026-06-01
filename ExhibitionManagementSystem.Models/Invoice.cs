using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Invoice : IAuditableEntity, ISoftDeletable
{
    [Key] public int InvoiceID { get; set; }
    public int TenantID { get; set; }
    public int ReservationID { get; set; }
    [Required, StringLength(50)] public string InvoiceNumber { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(18,2)")] public decimal SubTotal { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal TaxRate { get; set; } = 0;
    [Column(TypeName = "decimal(18,2)")] public decimal TaxAmount { get; set; } = 0;
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; }
    public InvoiceStatus Status { get; set; }
    [Column(TypeName = "date")] public DateTime? DueDate { get; set; }
    [StringLength(500)] public string Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new HashSet<Payment>();
}
