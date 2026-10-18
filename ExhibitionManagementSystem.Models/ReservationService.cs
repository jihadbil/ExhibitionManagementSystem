using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل خدمة إضافية مرتبطة بحجز جناح.
/// </summary>
public class ReservationService
{
    /// <summary>
    /// المعرف الفريد لخدمة الحجز.
    /// </summary>
    [Key] public int ReservationServiceID { get; set; }

    /// <summary>
    /// معرف الحجز المرتبط بالخدمة.
    /// </summary>
    public int ReservationID { get; set; }

    /// <summary>
    /// معرف الخدمة المطلوبة.
    /// </summary>
    public int ServiceID { get; set; }

    /// <summary>
    /// كمية الخدمة المطلوبة.
    /// </summary>
    [Column(TypeName = "decimal(10,2)")] public decimal Quantity { get; set; }

    /// <summary>
    /// سعر وحدة الخدمة وقت إضافتها للحجز.
    /// </summary>
    [Column(TypeName = "decimal(28,8)")] public decimal UnitPrice { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في تسعير الخدمة.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// إجمالي سعر الخدمة بعد ضرب الكمية في سعر الوحدة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; set; }

    /// <summary>سعر الوحدة كما ورد من قاعدة تسعير الخدمة قبل التحويل.</summary>
    [Column(TypeName = "decimal(18,2)")] public decimal SourceUnitPrice { get; set; }

    /// <summary>الإجمالي الأصلي للخدمة قبل التحويل.</summary>
    [Column(TypeName = "decimal(18,2)")] public decimal SourceTotalPrice { get; set; }

    /// <summary>عملة المصدر لقاعدة تسعير الخدمة.</summary>
    [Required, StringLength(3)] public string SourceCurrencyCode { get; set; } = "LYD";

    /// <summary>سعر التحويل من عملة المصدر إلى عملة الحجز.</summary>
    [Column(TypeName = "decimal(18,6)")] public decimal ExchangeRateUsed { get; set; } = 1;

    public DateTime? ExchangeRateDate { get; set; }

    [StringLength(100)] public string ExchangeRateSource { get; set; } = string.Empty;

    public int? ServicePriceRuleID { get; set; }

    /// <summary>
    /// الحجز المرتبط بالخدمة.
    /// </summary>
    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; } = null!;

    /// <summary>
    /// الخدمة المرتبطة بالحجز.
    /// </summary>
    [ForeignKey(nameof(ServiceID))] public virtual Service Service { get; set; } = null!;

    /// <summary>
    /// العملة المستخدمة في تسعير الخدمة.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(SourceCurrencyCode))] public virtual Currency SourceCurrency { get; set; } = null!;

}
