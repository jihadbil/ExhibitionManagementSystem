using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using ExhibitionManagementSystem.Models.Enums;

namespace ExhibitionManagementSystem.Models;

public class TicketScan
{
    [Key] 
    public int ScanID { get; set; }
    public int TicketID { get; set; }
    public DateTime ScanDateTime { get; set; } = DateTime.UtcNow;
    [StringLength(100)] 
    public string GateName { get; set; }
    public ScanDirection Direction { get; set; }
    [StringLength(450)] 
    public string ScannedByUserId { get; set; }

    [ForeignKey(nameof(TicketID))] 
    public virtual Ticket Ticket { get; set; }
    [ForeignKey(nameof(ScannedByUserId))] 
    public virtual ApplicationUser ScannedByUser { get; set; }

}
