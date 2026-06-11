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
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }

    /// <summary>
    /// رمز العملة المستخدمة في تسعير الخدمة.
    /// </summary>
    [Required, StringLength(3)] public string CurrencyCode { get; set; }

    /// <summary>
    /// إجمالي سعر الخدمة بعد ضرب الكمية في سعر الوحدة.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; set; }

    /// <summary>
    /// الحجز المرتبط بالخدمة.
    /// </summary>
    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }

    /// <summary>
    /// الخدمة المرتبطة بالحجز.
    /// </summary>
    [ForeignKey(nameof(ServiceID))] public virtual Service Service { get; set; }

    /// <summary>
    /// العملة المستخدمة في تسعير الخدمة.
    /// </summary>
    [ForeignKey(nameof(CurrencyCode))] public virtual Currency Currency { get; set; }

}
