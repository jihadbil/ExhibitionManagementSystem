using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

public class FinancialReport
{
    [Key] public int ReportID { get; set; }
    public int TenantID { get; set; }
    public int ExhibitionID { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalRevenue { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalExpenses { get; set; } = 0;
    [Column(TypeName = "decimal(18,2)")] public decimal NetProfit { get; set; }
    public int TotalVisitors { get; set; }
    public int TotalExhibitors { get; set; }
    public int TotalBooths { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal OccupancyRate { get; set; }
    [Required, StringLength(3)] public string CurrencyCode { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    [StringLength(450)] public string GeneratedByUserId { get; set; }
    [Column(TypeName = "date")] public DateTime? ReportPeriodFrom { get; set; }
    [Column(TypeName = "date")] public DateTime? ReportPeriodTo { get; set; }

    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }
    [ForeignKey(nameof(GeneratedByUserId))] public virtual ApplicationUser GeneratedByUser { get; set; }

}
