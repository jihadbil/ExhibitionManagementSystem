using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using System.Linq;
using ExhibitionManagementSystem.Models.Interfaces;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل مستأجرًا في النظام مع بيانات الاشتراك والعملة الأساسية.
/// </summary>
public class Tenant : IAuditableEntity
{
    /// <summary>
    /// المعرف الفريد للمستأجر.
    /// </summary>
    [Key] public int TenantID { get; set; }

    /// <summary>
    /// اسم الشركة أو الجهة المالكة للمستأجر.
    /// </summary>
    [Required, StringLength(200)] public string CompanyName { get; set; }

    /// <summary>
    /// النطاق الفرعي الخاص بالمستأجر.
    /// </summary>
    [StringLength(100)] public string Subdomain { get; set; }

    /// <summary>
    /// يحدد ما إذا كان المستأجر نشطًا في النظام.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// تاريخ انتهاء الفترة التجريبية للمستأجر.
    /// </summary>
    public DateTime? TrialEndsAt { get; set; }

    /// <summary>
    /// رمز العملة الأساسية للمستأجر.
    /// </summary>
    [StringLength(3)] public string BaseCurrency { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل المستأجر.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل المستأجر.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// اسم الخطة النشطة الحالية للمستأجر المحسوبة من الاشتراكات.
    /// </summary>
    [NotMapped]
    public string? CurrentPlan => TenantSubscriptions
        .Where(s => s.Status == SubscriptionStatus.Active)
        .OrderByDescending(s => s.StartDate)
        .FirstOrDefault()?.Plan;

    /// <summary>
    /// العملة الأساسية المرتبطة بالمستأجر.
    /// </summary>
    [ForeignKey(nameof(BaseCurrency))]
    public virtual Currency Currency { get; set; }

    /// <summary>
    /// اشتراكات المستأجر عبر الزمن.
    /// </summary>
    public virtual ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new HashSet<TenantSubscription>();
}
