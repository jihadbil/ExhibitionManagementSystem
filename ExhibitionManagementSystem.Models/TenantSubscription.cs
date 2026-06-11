using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل اشتراك مستأجر في خطة محددة خلال فترة زمنية.
/// </summary>
public class TenantSubscription : IAuditableEntity
{

    /// <summary>
    /// المعرف الفريد للاشتراك.
    /// </summary>
    [Key] public int SubID { get; set; }

    /// <summary>
    /// معرف المستأجر صاحب الاشتراك.
    /// </summary>
    public int TenantID { get; set; }

    /// <summary>
    /// اسم خطة الاشتراك.
    /// </summary>
    [Required, StringLength(50)] public string Plan { get; set; }

    /// <summary>
    /// تاريخ بداية الاشتراك.
    /// </summary>
    [Column(TypeName = "date")] public DateTime StartDate { get; set; }

    /// <summary>
    /// تاريخ نهاية الاشتراك.
    /// </summary>
    [Column(TypeName = "date")] public DateTime EndDate { get; set; }

    /// <summary>
    /// الرسوم الشهرية للاشتراك.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal MonthlyFee { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في رسوم الاشتراك.
    /// </summary>
    [StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// حالة الاشتراك الحالية.
    /// </summary>
    public SubscriptionStatus Status { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل الاشتراك.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل الاشتراك.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// المستأجر المرتبط بالاشتراك.
    /// </summary>
    [ForeignKey(nameof(TenantID))] public virtual Tenant Tenant { get; set; }

    /// <summary>
    /// العملة المستخدمة في رسوم الاشتراك.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

}
