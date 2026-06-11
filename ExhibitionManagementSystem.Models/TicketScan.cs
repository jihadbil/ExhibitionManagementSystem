using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

/// <summary>
/// يمثل عملية مسح لتذكرة عند بوابة دخول أو خروج.
/// </summary>
public class TicketScan
{
    /// <summary>
    /// المعرف الفريد لعملية المسح.
    /// </summary>
    [Key] 
    public int ScanID { get; set; }

    /// <summary>
    /// معرف التذكرة التي تم مسحها.
    /// </summary>
    public int TicketID { get; set; }

    /// <summary>
    /// تاريخ ووقت تنفيذ عملية المسح.
    /// </summary>
    public DateTime ScanDateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// اسم البوابة التي تمت عندها عملية المسح.
    /// </summary>
    [StringLength(100)] 
    public string GateName { get; set; }

    /// <summary>
    /// اتجاه الحركة المسجلة في عملية المسح.
    /// </summary>
    public ScanDirection Direction { get; set; }

    /// <summary>
    /// معرف المستخدم الذي نفذ عملية المسح.
    /// </summary>
    [StringLength(450)] 
    public string ScannedByUserId { get; set; }

    /// <summary>
    /// التذكرة المرتبطة بعملية المسح.
    /// </summary>
    [ForeignKey(nameof(TicketID))] 
    public virtual Ticket Ticket { get; set; }

    /// <summary>
    /// المستخدم الذي نفذ عملية المسح.
    /// </summary>
    [ForeignKey(nameof(ScannedByUserId))] 
    public virtual ApplicationUser ScannedByUser { get; set; }

}
