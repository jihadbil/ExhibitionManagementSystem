using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل سعر صرف بين عملتين في تاريخ محدد.
/// </summary>
public class ExchangeRate
{


    /// <summary>
    /// المعرف الفريد لسعر الصرف.
    /// </summary>
    [Key] public int RateID { get; set; }

    /// <summary>
    /// رمز العملة المصدر التي يتم التحويل منها.
    /// </summary>
    [Required, StringLength(3)] public string FromCurrency { get; set; }

    /// <summary>
    /// رمز العملة الهدف التي يتم التحويل إليها.
    /// </summary>
    [Required, StringLength(3)] public string ToCurrency { get; set; }

    /// <summary>
    /// قيمة سعر الصرف بين العملتين.
    /// </summary>
    [Column(TypeName = "decimal(18,6)")] public decimal Rate { get; set; }

    /// <summary>
    /// التاريخ الذي ينطبق عليه سعر الصرف.
    /// </summary>
    [Column(TypeName = "date")] public DateTime RateDate { get; set; }

    /// <summary>
    /// مصدر سعر الصرف أو الجهة التي تم الاعتماد عليها.
    /// </summary>
    [Required, StringLength(50)] public string Source { get; set; }

    /// <summary>
    /// معرف المستخدم الذي أضاف سعر الصرف.
    /// </summary>
    [StringLength(450)] public string CreatedByUserId { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل سعر الصرف.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// العملة المصدر المرتبطة بسعر الصرف.
    /// </summary>
    [ForeignKey(nameof(FromCurrency))] public virtual Currency FromCurrencyNav { get; set; }

    /// <summary>
    /// العملة الهدف المرتبطة بسعر الصرف.
    /// </summary>
    [ForeignKey(nameof(ToCurrency))] public virtual Currency ToCurrencyNav { get; set; }

    /// <summary>
    /// المستخدم الذي أضاف سعر الصرف.
    /// </summary>
    [ForeignKey(nameof(CreatedByUserId))] public virtual ApplicationUser CreatedByUser { get; set; }

}
