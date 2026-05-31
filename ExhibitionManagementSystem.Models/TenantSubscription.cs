using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

public class TenantSubscription : IAuditableEntity
{

    [Key] public int SubID { get; set; }
    public int TenantID { get; set; }
    [Required, StringLength(50)] public string Plan { get; set; }
    [Column(TypeName = "date")] public DateTime StartDate { get; set; }
    [Column(TypeName = "date")] public DateTime EndDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal MonthlyFee { get; set; }
    [StringLength(3)] public string CurrencyCode { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

}
