using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل فعالية أو جلسة مجدولة ضمن معرض.
/// </summary>
public class ExhibitionSchedule
{


    /// <summary>
    /// المعرف الفريد للجدولة.
    /// </summary>
    [Key] public int ScheduleID { get; set; }

    /// <summary>
    /// معرف المعرض الذي تنتمي إليه الفعالية.
    /// </summary>
    public int ExhibitionID { get; set; }

    /// <summary>
    /// معرف القاعة التي تقام فيها الفعالية عند تحديدها.
    /// </summary>
    public int? HallID { get; set; }

    /// <summary>
    /// اسم الفعالية أو الجلسة.
    /// </summary>
    [Required, StringLength(200)] public string EventName { get; set; }

    /// <summary>
    /// نوع الفعالية مثل محاضرة أو ورشة.
    /// </summary>
    public EventType? EventType { get; set; }

    /// <summary>
    /// تاريخ ووقت بداية الفعالية.
    /// </summary>
    public DateTime StartDateTime { get; set; }

    /// <summary>
    /// تاريخ ووقت نهاية الفعالية.
    /// </summary>
    public DateTime EndDateTime { get; set; }

    /// <summary>
    /// اسم المتحدث أو مقدم الفعالية.
    /// </summary>
    [StringLength(200)] public string SpeakerName { get; set; }

    /// <summary>
    /// الحد الأقصى لعدد الحضور.
    /// </summary>
    public int? MaxAttendees { get; set; }

    /// <summary>
    /// وصف محتوى الفعالية.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// يحدد ما إذا كانت الفعالية متاحة للجمهور.
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// المعرض المرتبط بالفعالية.
    /// </summary>
    [ForeignKey(nameof(ExhibitionID))] public virtual Exhibition Exhibition { get; set; }

    /// <summary>
    /// القاعة المرتبطة بالفعالية.
    /// </summary>
    [ForeignKey(nameof(HallID))] public virtual Hall Hall { get; set; }

}
