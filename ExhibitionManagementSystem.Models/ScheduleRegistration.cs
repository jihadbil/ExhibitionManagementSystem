using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Interfaces;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل تسجيل زائر في فعالية مجدولة ضمن معرض.
/// </summary>
public class ScheduleRegistration : IAuditableEntity
{

    /// <summary>
    /// المعرف الفريد لتسجيل الفعالية.
    /// </summary>
    [Key] public int RegID { get; set; }

    /// <summary>
    /// معرف الفعالية المجدولة.
    /// </summary>
    public int ScheduleID { get; set; }

    /// <summary>
    /// معرف الزائر المسجل في الفعالية.
    /// </summary>
    public int VisitorID { get; set; }

    /// <summary>
    /// تاريخ ووقت تسجيل الزائر في الفعالية.
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// حالة تسجيل الزائر في الفعالية.
    /// </summary>
    public RegistrationStatus Status { get; set; }

    /// <summary>
    /// تاريخ إنشاء سجل التسجيل.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// تاريخ آخر تعديل على سجل التسجيل.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// الفعالية المجدولة المرتبطة بالتسجيل.
    /// </summary>
    [ForeignKey(nameof(ScheduleID))] public virtual ExhibitionSchedule Schedule { get; set; }

    /// <summary>
    /// الزائر المسجل في الفعالية.
    /// </summary>
    [ForeignKey(nameof(VisitorID))] public virtual Visitor Visitor { get; set; }

}
