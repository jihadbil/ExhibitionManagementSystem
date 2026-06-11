using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل عضو فريق يعمل ضمن جناح محجوز.
/// </summary>
public class BoothStaff : IAuditableEntity, ISoftDeletable
{
    /// <summary>
    /// المعرف الفريد لعضو الفريق.
    /// </summary>
    [Key] public int StaffID { get; set; }

    /// <summary>
    /// معرف الحجز الذي يعمل ضمنه عضو الفريق.
    /// </summary>
    public int ReservationID { get; set; }

    /// <summary>
    /// اسم عضو الفريق.
    /// </summary>
    [Required, StringLength(100)] public string StaffName { get; set; }

    /// <summary>
    /// الدور أو الوظيفة داخل الجناح.
    /// </summary>
    [StringLength(50)] public string Role { get; set; }

    /// <summary>
    /// رقم هاتف عضو الفريق.
    /// </summary>
    [StringLength(20)] public string Phone { get; set; }

    /// <summary>
    /// البريد الإلكتروني لعضو الفريق.
    /// </summary>
    [StringLength(200)] public string Email { get; set; }

    /// <summary>
    /// يحدد ما إذا تم إصدار شارة دخول لعضو الفريق.
    /// </summary>
    public bool BadgeIssued { get; set; } = false;

    /// <summary>
    /// رقم شارة الدخول الصادرة لعضو الفريق.
    /// </summary>
    [StringLength(50)] public string BadgeNumber { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل عضو الفريق.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل عضو الفريق.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// يحدد ما إذا كان سجل عضو الفريق محذوفًا حذفًا ناعمًا.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// تاريخ تنفيذ الحذف الناعم.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// معرف المستخدم الذي نفذ الحذف الناعم.
    /// </summary>
    public string? DeletedByUserId { get; set; }

    /// <summary>
    /// الحجز المرتبط بعضو الفريق.
    /// </summary>
    [ForeignKey(nameof(ReservationID))] public virtual BoothReservation Reservation { get; set; }
}
