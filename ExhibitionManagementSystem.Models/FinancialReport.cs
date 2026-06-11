using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل تقريرًا ماليًا ملخصًا لمعرض ضمن فترة محددة.
/// </summary>
public class FinancialReport
{
    /// <summary>
    /// المعرف الفريد للتقرير المالي.
    /// </summary>
    [Key] public int ReportID { get; set; }

    /// <summary>
    /// معرف المستأجر الذي يخصه التقرير.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// معرف المعرض الذي يغطيه التقرير.
    /// </summary>
    public int ExhibitionID { get; set; }

    /// <summary>
    /// إجمالي الإيرادات المسجلة للمعرض.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal TotalRevenue { get; set; }

    /// <summary>
    /// إجمالي المصروفات المسجلة للمعرض.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal TotalExpenses { get; set; } = 0;

    /// <summary>
    /// صافي الربح بعد طرح المصروفات من الإيرادات.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal NetProfit { get; set; }

    /// <summary>
    /// إجمالي عدد الزوار المسجلين أو المحتسبين في التقرير.
    /// </summary>
    public int TotalVisitors { get; set; }

    /// <summary>
    /// إجمالي عدد العارضين المشاركين.
    /// </summary>
    public int TotalExhibitors { get; set; }

    /// <summary>
    /// إجمالي عدد الأجنحة ضمن التقرير.
    /// </summary>
    public int TotalBooths { get; set; }

    /// <summary>
    /// نسبة إشغال الأجنحة أو المساحات.
    /// </summary>
    [Column(TypeName = "decimal(5,2)")] public decimal OccupancyRate { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في قيم التقرير.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// تاريخ ووقت توليد التقرير.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// معرف المستخدم الذي ولّد التقرير.
    /// </summary>
    [StringLength(450)] public string GeneratedByUserId { get; set; }

    /// <summary>
    /// بداية الفترة التي يغطيها التقرير.
    /// </summary>
    [Column(TypeName = "date")] public DateTime? ReportPeriodFrom { get; set; }

    /// <summary>
    /// نهاية الفترة التي يغطيها التقرير.
    /// </summary>
    [Column(TypeName = "date")] public DateTime? ReportPeriodTo { get; set; }

    /// <summary>
    /// المستأجر المرتبط بالتقرير.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// المعرض المرتبط بالتقرير.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// العملة المستخدمة في التقرير.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

    /// <summary>
    /// المستخدم الذي ولّد التقرير.
    /// </summary>
    [ForeignKey(nameof(GeneratedByUserId))] public virtual ApplicationUser GeneratedByUser { get; set; }

}
