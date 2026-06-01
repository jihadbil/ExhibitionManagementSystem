using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

public class Expense : IAuditableEntity, ISoftDeletable
{
    [Key] public int ExpenseID { get; set; }
    public int TenantID { get; set; }
    public int ExhibitionID { get; set; }
    [Required, StringLength(200)] public string Description { get; set; }
    [Required, StringLength(100)] public string Category { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; }
    [Column(TypeName = "date")] public DateTime ExpenseDate { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    [StringLength(450)] public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedByUserId { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }
    [ForeignKey(nameof(CreatedByUserId))] public virtual ApplicationUser? CreatedByUser { get; set; }
}
